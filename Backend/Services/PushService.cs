using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Backend.Data;
using Backend.Models;
using Lib.Net.Http.WebPush;
using Lib.Net.Http.WebPush.Authentication;

namespace Backend.Services;

public record PushBericht(string Titel, string Tekst, string Url, string Tag);

// Stuurt pushmeldingen naar alle apparaten van een gebruiker
public class PushService
{
    private const string Onderwerp = "https://www.racecoachfinder.nl";
    private static VapidAuthentication? _vapid;
    // Eén gedeelde client (en HttpClient) voor alle meldingen; gebruikt standaard aes128gcm (nodig voor iPhone)
    private static readonly PushServiceClient _client = new(new HttpClient());
    private static readonly SemaphoreSlim _vapidSlot = new(1, 1);

    private readonly AppDbContext _context;
    private readonly ILogger<PushService> _logger;

    public PushService(AppDbContext context, ILogger<PushService> logger)
    {
        _context = context;
        _logger = logger;
    }

    // Sleutels uit omgevingsvariabelen, anders uit de database; bestaan ze nog niet, dan eenmalig aanmaken
    public async Task<VapidAuthentication> GetVapidAsync()
    {
        if (_vapid != null) return _vapid;
        await _vapidSlot.WaitAsync();
        try
        {
            if (_vapid != null) return _vapid;
            var pub = Environment.GetEnvironmentVariable("VAPID_PUBLIC_KEY");
            var priv = Environment.GetEnvironmentVariable("VAPID_PRIVATE_KEY");
            if (string.IsNullOrEmpty(pub) || string.IsNullOrEmpty(priv))
            {
                var opgeslagen = await _context.AppInstellingen
                    .Where(i => i.Sleutel == "VapidPublic" || i.Sleutel == "VapidPrivate")
                    .ToDictionaryAsync(i => i.Sleutel, i => i.Waarde);
                if (opgeslagen.TryGetValue("VapidPublic", out var p) && opgeslagen.TryGetValue("VapidPrivate", out var q))
                {
                    pub = p; priv = q;
                }
                else
                {
                    (pub, priv) = MaakVapidSleutels();
                    _context.AppInstellingen.Add(new AppInstelling { Sleutel = "VapidPublic", Waarde = pub });
                    _context.AppInstellingen.Add(new AppInstelling { Sleutel = "VapidPrivate", Waarde = priv });
                    await _context.SaveChangesAsync();
                }
            }
            _vapid = new VapidAuthentication(pub, priv) { Subject = Onderwerp };
            return _vapid;
        }
        finally { _vapidSlot.Release(); }
    }

    // P-256 sleutelpaar in het webpush-formaat: publiek = ongecomprimeerd punt, privé = D (beide base64url)
    private static (string publiek, string prive) MaakVapidSleutels()
    {
        using var ecdsa = System.Security.Cryptography.ECDsa.Create(System.Security.Cryptography.ECCurve.NamedCurves.nistP256);
        var p = ecdsa.ExportParameters(true);
        var punt = new byte[65];
        punt[0] = 0x04;
        p.Q.X!.CopyTo(punt, 1);
        p.Q.Y!.CopyTo(punt, 33);
        static string B64(byte[] b) => Convert.ToBase64String(b).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        return (B64(punt), B64(p.D!));
    }

    public async Task<int> StuurAsync(IEnumerable<string> gebruikerIds, PushBericht bericht)
    {
        var ids = gebruikerIds.Distinct().ToList();
        if (ids.Count == 0) return 0;
        var abonnementen = await _context.PushAbonnementen.Where(a => ids.Contains(a.GebruikerId)).ToListAsync();
        if (abonnementen.Count == 0) return 0;

        var vapid = await GetVapidAsync();
        var payload = JsonSerializer.Serialize(new { titel = bericht.Titel, tekst = bericht.Tekst, url = bericht.Url, tag = bericht.Tag });
        var verstuurd = 0;

        foreach (var a in abonnementen)
        {
            try
            {
                var abonnement = new PushSubscription { Endpoint = a.Endpoint };
                abonnement.SetKey(PushEncryptionKeyName.P256DH, a.P256dh);
                abonnement.SetKey(PushEncryptionKeyName.Auth, a.Auth);
                await _client.RequestPushMessageDeliveryAsync(abonnement,
                    new PushMessage(payload) { TimeToLive = 60 * 60 * 24, Urgency = PushMessageUrgency.High }, vapid);
                verstuurd++;
            }
            catch (PushServiceClientException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Gone || ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                // Apparaat heeft meldingen uitgezet of bestaat niet meer: opruimen
                _context.PushAbonnementen.Remove(a);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Pushmelding mislukt: {Fout}", ex.Message);
            }
        }
        await _context.SaveChangesAsync();
        return verstuurd;
    }
}

// Wachtrij tussen 'bericht opgeslagen' en 'melding versturen', zodat het opslaan niet hoeft te wachten
public record PushTaak(string Soort, int Id);

public class PushWachtrij
{
    private readonly Channel<PushTaak> _kanaal = Channel.CreateUnbounded<PushTaak>();
    public void Voeg(PushTaak taak) => _kanaal.Writer.TryWrite(taak);
    public IAsyncEnumerable<PushTaak> LeesAlles(CancellationToken ct) => _kanaal.Reader.ReadAllAsync(ct);
}

// Ziet elk nieuw (groeps)bericht dat wordt opgeslagen, waar in de code dat ook gebeurt
public class PushMeldingInterceptor : SaveChangesInterceptor
{
    private readonly PushWachtrij _wachtrij;
    private static readonly ConditionalWeakTable<DbContext, List<object>> _nieuw = new();

    public PushMeldingInterceptor(PushWachtrij wachtrij) => _wachtrij = wachtrij;

    private static void Verzamel(DbContext? context)
    {
        if (context == null) return;
        var lijst = _nieuw.GetOrCreateValue(context);
        lijst.AddRange(context.ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added && (e.Entity is Bericht || e.Entity is Groepsbericht))
            .Select(e => e.Entity));
    }

    private void Verstuur(DbContext? context)
    {
        if (context == null || !_nieuw.TryGetValue(context, out var lijst)) return;
        foreach (var e in lijst)
        {
            if (e is Bericht b && b.Id > 0) _wachtrij.Voeg(new PushTaak("bericht", b.Id));
            else if (e is Groepsbericht g && g.Id > 0) _wachtrij.Voeg(new PushTaak("groep", g.Id));
        }
        lijst.Clear();
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Verzamel(eventData.Context);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken ct = default)
    {
        Verzamel(eventData.Context);
        return ValueTask.FromResult(result);
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        Verstuur(eventData.Context);
        return result;
    }

    public override ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result, CancellationToken ct = default)
    {
        Verstuur(eventData.Context);
        return ValueTask.FromResult(result);
    }
}

// Verwerkt de wachtrij op de achtergrond en bepaalt per bericht wie een melding krijgt
public class PushVerzender : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly PushWachtrij _wachtrij;
    private readonly ILogger<PushVerzender> _logger;

    public PushVerzender(IServiceProvider services, PushWachtrij wachtrij, ILogger<PushVerzender> logger)
    {
        _services = services;
        _wachtrij = wachtrij;
        _logger = logger;
    }

    private static string Kort(string tekst, int max = 120)
    {
        tekst = tekst.Replace("\n", " ").Trim();
        return tekst.Length > max ? tekst[..max] + "…" : tekst;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var taak in _wachtrij.LeesAlles(stoppingToken))
        {
            try
            {
                using var scope = _services.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var push = scope.ServiceProvider.GetRequiredService<PushService>();

                if (taak.Soort == "bericht")
                {
                    var b = await context.Berichten.FindAsync(taak.Id);
                    if (b == null) continue;
                    var afzender = await context.Users.Where(u => u.Id == b.VanGebruikerId).Select(u => u.Naam).FirstOrDefaultAsync() ?? "Iemand";
                    string titel, tekst;
                    if (b.Tekst.StartsWith("🎯 UITNODIGING:"))
                    {
                        titel = $"Uitnodiging van {afzender}";
                        tekst = Kort(string.Join(" ", b.Tekst.Split('\n').Skip(1)));
                    }
                    else
                    {
                        titel = afzender;
                        tekst = Kort(b.Tekst);
                    }
                    await push.StuurAsync(new[] { b.NaarGebruikerId }, new PushBericht(
                        titel, tekst, $"/berichten.html?partner={Uri.EscapeDataString(b.VanGebruikerId)}", $"chat-{b.VanGebruikerId}"));
                }
                else if (taak.Soort == "groep")
                {
                    var g = await context.Groepsberichten.FindAsync(taak.Id);
                    if (g == null) continue;
                    var groep = await context.Groepsgesprekken.FindAsync(g.GroepsgesprekId);
                    var leden = await context.GroepsgesprekLeden
                        .Where(l => l.GroepsgesprekId == g.GroepsgesprekId && l.GebruikerId != g.VanGebruikerId)
                        .Select(l => l.GebruikerId)
                        .ToListAsync();
                    await push.StuurAsync(leden, new PushBericht(
                        groep?.Naam ?? "Groepsgesprek", Kort($"{g.VanNaam}: {g.Tekst}"),
                        $"/berichten.html?groep={g.GroepsgesprekId}", $"groep-{g.GroepsgesprekId}"));
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Pushmelding verwerken mislukt: {Fout}", ex.Message);
            }
        }
    }
}
