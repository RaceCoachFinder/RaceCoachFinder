using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using Backend.Data;

namespace Backend.Services;

// Mail + pushmelding naar een coach die van een admin een gratis periode krijgt.
// Er wordt even gewacht, zodat er niets wordt verstuurd als de admin het meteen ongedaan maakt.
// Meerdere wijzigingen kort na elkaar worden samen één melding (met de uiteindelijke einddatum).
public record GratisMeldingTaak(string GebruikerId, DateTime? Vorige, DateTime Nieuwe);

public class GratisMeldingWachtrij
{
    public static readonly TimeSpan Wachttijd = TimeSpan.FromMinutes(2);

    private readonly Channel<GratisMeldingTaak> _kanaal = Channel.CreateUnbounded<GratisMeldingTaak>();
    // Per coach: de einddatum van vóór de eerste nog niet gemelde wijziging + aantal open taken
    private readonly Dictionary<string, (DateTime? Origineel, int Open)> _open = new();
    private readonly object _slot = new();

    public void Voeg(GratisMeldingTaak taak)
    {
        lock (_slot)
        {
            _open[taak.GebruikerId] = _open.TryGetValue(taak.GebruikerId, out var o)
                ? (o.Origineel, o.Open + 1)
                : (taak.Vorige, 1);
        }
        _kanaal.Writer.TryWrite(taak);
    }

    public IAsyncEnumerable<GratisMeldingTaak> LeesAlles(CancellationToken ct) => _kanaal.Reader.ReadAllAsync(ct);

    // Taak klaar: geeft de einddatum van vóór de eerste openstaande wijziging terug
    public DateTime? RondAf(GratisMeldingTaak taak)
    {
        lock (_slot)
        {
            if (!_open.TryGetValue(taak.GebruikerId, out var o)) return taak.Vorige;
            if (o.Open <= 1) _open.Remove(taak.GebruikerId);
            else _open[taak.GebruikerId] = (o.Origineel, o.Open - 1);
            return o.Origineel;
        }
    }
}

public class GratisMeldingVerzender : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly GratisMeldingWachtrij _wachtrij;
    private readonly ILogger<GratisMeldingVerzender> _logger;

    public GratisMeldingVerzender(IServiceProvider services, GratisMeldingWachtrij wachtrij, ILogger<GratisMeldingVerzender> logger)
    {
        _services = services;
        _wachtrij = wachtrij;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var taak in _wachtrij.LeesAlles(stoppingToken))
            _ = VerwerkAsync(taak, stoppingToken);
    }

    private async Task VerwerkAsync(GratisMeldingTaak taak, CancellationToken ct)
    {
        try { await Task.Delay(GratisMeldingWachtrij.Wachttijd, ct); }
        catch (OperationCanceledException) { return; }

        var origineel = _wachtrij.RondAf(taak);
        try
        {
            using var scope = _services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var gebruiker = await context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == taak.GebruikerId, ct);
            // Ongedaan gemaakt, of daarna nog eens aangepast (dan meldt die latere wijziging het)
            if (gebruiker?.GratisVerlooptOp == null || gebruiker.GratisVerlooptOp.Value != taak.Nieuwe) return;

            var nieuwe = gebruiker.GratisVerlooptOp.Value;
            var nu = DateTime.UtcNow;
            if (nieuwe <= nu) return;
            var altijd = nieuwe.Year >= 9999;
            var verlengd = !altijd && origineel.HasValue && origineel.Value > nu;
            var taal = await EmailTaal.VoorAsync(context, gebruiker.Id);
            var titel = "🎁 " + EmailTemplates.GratisPeriodeTitel(nieuwe, verlengd, taal);
            var tekst = EmailTemplates.GratisPeriodeKort(nieuwe, taal);

            var email = gebruiker.Email;
            if (!string.IsNullOrWhiteSpace(email) && email.Contains('@'))
            {
                try
                {
                    var mail = scope.ServiceProvider.GetRequiredService<IEmailService>();
                    await mail.VerstuurAsync(email, gebruiker.Naam, titel.Replace("🎁 ", "") + " 🎁",
                        EmailTemplates.GratisPeriode(gebruiker.Naam, nieuwe, verlengd, taal));
                }
                catch (Exception ex) { _logger.LogWarning("Mail gratis periode mislukt: {Fout}", ex.Message); }
            }

            try
            {
                var push = scope.ServiceProvider.GetRequiredService<PushService>();
                await push.StuurAsync(new[] { gebruiker.Id }, new PushBericht(titel, tekst, "/dashboard-coach.html", "gratis-periode"));
            }
            catch (Exception ex) { _logger.LogWarning("Pushmelding gratis periode mislukt: {Fout}", ex.Message); }
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Melding gratis periode mislukt: {Fout}", ex.Message);
        }
    }
}
