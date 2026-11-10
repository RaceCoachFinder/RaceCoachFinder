using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Mollie.Api.Client;
using Mollie.Api.Models;
using Mollie.Api.Models.Payment.Request;
using Mollie.Api.Models.Payment.Response;
using Backend.Data;
using Backend.Models;

namespace Backend.Services;

public enum KoppelStatus { NietIngesteld, NietGekoppeld, Ingetrokken, NietGeactiveerd, Klaar }

// Mollie Connect: coaches koppelen hun eigen Mollie-account. Een rijder betaalt een factuur dan
// rechtstreeks aan de coach, en de servicekosten (2%) gaan als application fee automatisch naar
// het Mollie-account van RaceCoachFinder.
//
// Instellen in Railway:
//   MOLLIE_CLIENT_ID       app_... (Mollie → Ontwikkelaars → Jouw apps)
//   MOLLIE_CLIENT_SECRET   het geheim van die app
//   MOLLIE_CONNECT_TESTMODUS  "true" om eerst testbetalingen te doen (optioneel)
//   MOLLIE_REDIRECT_URI    alleen nodig als de backend een ander adres krijgt
public class MollieConnectService
{
    public const string HttpNaam = "mollie";
    private const string TokenUrl = "https://api.mollie.com/oauth2/tokens";
    private static readonly string[] Rechten =
        { "payments.read", "payments.write", "profiles.read", "organizations.read", "onboarding.read" };

    private readonly AppDbContext _context;
    private readonly IHttpClientFactory _http;
    private readonly IDataProtector _tokens;
    private readonly ITimeLimitedDataProtector _state;
    private readonly ILogger<MollieConnectService> _logger;

    public MollieConnectService(AppDbContext context, IHttpClientFactory http, IDataProtectionProvider dp, ILogger<MollieConnectService> logger)
    {
        _context = context;
        _http = http;
        _tokens = dp.CreateProtector("MollieConnect.Tokens");
        _state = dp.CreateProtector("MollieConnect.State").ToTimeLimitedDataProtector();
        _logger = logger;
    }

    private static string? Env(string naam) =>
        Environment.GetEnvironmentVariable(naam) is { Length: > 0 } v ? v.Trim() : null;

    private static string? ClientId => Env("MOLLIE_CLIENT_ID");
    private static string? ClientSecret => Env("MOLLIE_CLIENT_SECRET");
    public static bool IsIngesteld => ClientId != null && ClientSecret != null;
    public static bool Testmodus => Env("MOLLIE_CONNECT_TESTMODUS")?.ToLowerInvariant() is "true" or "1" or "ja";
    public static string RedirectUri =>
        Env("MOLLIE_REDIRECT_URI") ?? "https://racecoachfinder-production.up.railway.app/api/mollieconnect/terug";
    // Alleen voor lokaal testen (Development): alle Mollie-verkeer naar een nep-server
    public static string? NepUrl { get; set; }

    // Servicekosten: 2% van het factuurbedrag (afgerond op centen), minimaal 1 cent
    public static decimal Servicekosten(decimal bedrag) =>
        Math.Max(0.01m, Math.Round(bedrag * 0.02m, 2, MidpointRounding.AwayFromZero));

    public static string Bedrag(decimal d) => d.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);

    // ---------- Koppelen ----------

    public string MaakAutorisatieUrl(string gebruikerId, bool nieuwAccount, string taal)
    {
        var basis = (NepUrl ?? "https://my.mollie.com") + "/oauth2/authorize";
        var velden = new Dictionary<string, string>
        {
            ["client_id"] = ClientId!,
            ["redirect_uri"] = RedirectUri,
            ["state"] = _state.Protect(gebruikerId, TimeSpan.FromMinutes(30)),
            ["scope"] = string.Join(' ', Rechten),
            ["response_type"] = "code",
            ["approval_prompt"] = "auto",
            ["locale"] = taal == "en" ? "en_US" : "nl_NL",
            ["landing_page"] = nieuwAccount ? "signup" : "login"
        };
        return basis + "?" + string.Join("&", velden.Select(v => v.Key + "=" + Uri.EscapeDataString(v.Value)));
    }

    public string? GebruikerUitState(string? state)
    {
        if (string.IsNullOrEmpty(state)) return null;
        try { return _state.Unprotect(state); }
        catch { return null; }
    }

    // null = gelukt, anders een foutmelding
    public async Task<string?> VerwerkTerugkeerAsync(string gebruikerId, string code)
    {
        var (token, _) = await TokenAanvraagAsync(new()
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = RedirectUri
        });
        if (token == null || string.IsNullOrEmpty(token.RefreshToken))
            return "Mollie heeft de koppeling niet bevestigd. Probeer het opnieuw.";

        var k = await _context.MollieKoppelingen.FirstOrDefaultAsync(x => x.GebruikerId == gebruikerId);
        if (k == null)
        {
            k = new MollieKoppeling { GebruikerId = gebruikerId };
            _context.MollieKoppelingen.Add(k);
        }
        else
        {
            // Opnieuw gekoppeld (misschien met een ander Mollie-account): oude toegang intrekken
            var oud = Ontsleutel(k.RefreshTokenVersleuteld);
            if (oud != null && oud != token.RefreshToken) await IntrekkenAsync(oud);
        }
        k.RefreshTokenVersleuteld = _tokens.Protect(token.RefreshToken);
        k.AccessTokenVersleuteld = _tokens.Protect(token.AccessToken);
        k.AccessTokenVerlooptOp = DateTime.UtcNow.AddSeconds(Math.Max(60, token.ExpiresIn - 60));
        k.ProfielId = null;
        k.OnboardingStatus = null;
        k.KanBetalingenOntvangen = false;
        k.GekoppeldOp = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        try { await VernieuwStatusAsync(k); }
        catch (Exception ex) { _logger.LogWarning("Mollie-status ophalen na koppelen mislukt: {Fout}", ex.Message); }
        return null;
    }

    public async Task OntkoppelAsync(string gebruikerId)
    {
        var k = await _context.MollieKoppelingen.FirstOrDefaultAsync(x => x.GebruikerId == gebruikerId);
        if (k == null) return;
        var refresh = Ontsleutel(k.RefreshTokenVersleuteld);
        if (refresh != null) await IntrekkenAsync(refresh);
        _context.MollieKoppelingen.Remove(k);
        await _context.SaveChangesAsync();
    }

    // ---------- Status ----------

    public Task<MollieKoppeling?> KoppelingAsync(string gebruikerId) =>
        _context.MollieKoppelingen.FirstOrDefaultAsync(x => x.GebruikerId == gebruikerId);

    // Haalt naam, aanmeldstatus en websiteprofiel op bij Mollie
    public async Task VernieuwStatusAsync(MollieKoppeling k)
    {
        if (await AccessTokenAsync(k) == null) return;
        await MetTokenAsync(k, async (token, http) =>
        {
            using var onboardingClient = new OnboardingClient(token, http);
            var onboarding = await onboardingClient.GetOnboardingStatusAsync();
            k.OrganisatieNaam = onboarding.Name;
            k.OnboardingStatus = onboarding.Status;
            k.KanBetalingenOntvangen = onboarding.CanReceivePayments;
            if (string.IsNullOrEmpty(k.ProfielId))
            {
                using var profielClient = new ProfileClient(token, http);
                var profielen = await profielClient.GetProfileListAsync();
                k.ProfielId = profielen.Items
                    .Where(p => p.Status != "blocked")
                    .OrderBy(p => p.Status == "verified" ? 0 : 1)
                    .Select(p => p.Id)
                    .FirstOrDefault();
            }
            return true;
        });
        k.BijgewerktOp = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    // Kan deze coach nu online betaald worden? Bij twijfel wordt de status opnieuw bij Mollie opgevraagd.
    public async Task<KoppelStatus> StatusVoorAsync(string coachId, bool vernieuw = false)
    {
        if (!IsIngesteld) return KoppelStatus.NietIngesteld;
        var k = await KoppelingAsync(coachId);
        if (k == null) return KoppelStatus.NietGekoppeld;
        if (vernieuw || !k.KanBetalingenOntvangen || string.IsNullOrEmpty(k.ProfielId))
        {
            try { await VernieuwStatusAsync(k); }
            catch (Exception ex) { _logger.LogWarning("Mollie-status vernieuwen mislukt: {Fout}", ex.Message); }
        }
        if (k.OnboardingStatus == "ingetrokken") return KoppelStatus.Ingetrokken;
        if (!k.KanBetalingenOntvangen || string.IsNullOrEmpty(k.ProfielId)) return KoppelStatus.NietGeactiveerd;
        return KoppelStatus.Klaar;
    }

    public static string MeldingVoorCoach(KoppelStatus s) => s switch
    {
        KoppelStatus.NietIngesteld => "Online betalen is nog niet beschikbaar op RaceCoachFinder. Probeer het later opnieuw.",
        KoppelStatus.NietGekoppeld => "Koppel eerst je Mollie-account (Instellingen → Betalingen ontvangen). Daarna betalen rijders je facturen rechtstreeks aan jou.",
        KoppelStatus.Ingetrokken => "De koppeling met je Mollie-account werkt niet meer. Koppel je Mollie-account opnieuw via Instellingen → Betalingen ontvangen.",
        KoppelStatus.NietGeactiveerd => "Je Mollie-account is nog niet klaar om betalingen te ontvangen. Rond je aanmelding bij Mollie af; daarna kun je facturen sturen.",
        _ => ""
    };

    // Hoe wordt deze coach betaald? Werkend Mollie-account → online; anders via overmaken, als er een rekeningnummer is.
    // Overmaken kan dus alleen zolang een coach (nog) geen werkend Mollie-account heeft.
    public async Task<(BetaalKeuze? keuze, string? fout)> BetaalKeuzeAsync(string coachId)
    {
        var status = await StatusVoorAsync(coachId);
        if (status == KoppelStatus.Klaar) return (new BetaalKeuze(BetaalKeuze.Online, null, null), null);
        var coach = await _context.Coaches.AsNoTracking().FirstOrDefaultAsync(c => c.GebruikerId == coachId);
        if (!string.IsNullOrEmpty(coach?.Iban) && !string.IsNullOrWhiteSpace(coach.IbanTenaamstelling))
            return (new BetaalKeuze(BetaalKeuze.Overmaken, coach.Iban, coach.IbanTenaamstelling), null);
        return (null, MeldingGeenBetaalWijze(status));
    }

    public static string MeldingGeenBetaalWijze(KoppelStatus s) => s switch
    {
        KoppelStatus.NietIngesteld => "Vul eerst je rekeningnummer in (Instellingen → Betalingen ontvangen), zodat rijders je kunnen betalen.",
        KoppelStatus.Ingetrokken => "De koppeling met je Mollie-account werkt niet meer. Koppel Mollie opnieuw of vul je rekeningnummer in (Instellingen → Betalingen ontvangen).",
        KoppelStatus.NietGeactiveerd => "Je Mollie-account kan nog geen betalingen ontvangen. Rond je aanmelding bij Mollie af, of vul voorlopig je rekeningnummer in (Instellingen → Betalingen ontvangen).",
        _ => "Kies eerst hoe rijders je betalen (Instellingen → Betalingen ontvangen): koppel je Mollie-account of vul je rekeningnummer in."
    };

    // Coaches met een Mollie-account dat betalingen kan ontvangen: "Zakelijk geverifieerd"
    public async Task<HashSet<string>> ZakelijkGeverifieerdAsync(IEnumerable<string?> coachIds)
    {
        if (!IsIngesteld) return new HashSet<string>();
        var ids = coachIds.Where(i => !string.IsNullOrEmpty(i)).Distinct().ToList();
        var lijst = await _context.MollieKoppelingen
            .Where(k => ids.Contains(k.GebruikerId) && k.KanBetalingenOntvangen && k.ProfielId != null)
            .Select(k => k.GebruikerId).ToListAsync();
        return lijst.ToHashSet();
    }

    public static string MeldingVoorRijder(KoppelStatus s) => s == KoppelStatus.NietIngesteld
        ? "Online betalen is op dit moment niet beschikbaar. Probeer het later opnieuw."
        : "Deze coach kan op dit moment nog geen online betalingen ontvangen. Neem contact op met je coach.";

    // ---------- Betalingen ----------

    public async Task<PaymentResponse> MaakBetalingAsync(string coachId, PaymentRequest verzoek, decimal servicekosten, string omschrijvingKosten)
    {
        var k = await KoppelingAsync(coachId) ?? throw new InvalidOperationException("Coach heeft geen Mollie-koppeling.");
        if (string.IsNullOrEmpty(k.ProfielId)) await VernieuwStatusAsync(k);
        if (string.IsNullOrEmpty(k.ProfielId)) throw new InvalidOperationException("Geen websiteprofiel in het Mollie-account van de coach.");

        verzoek.ProfileId = k.ProfielId;
        verzoek.Testmode = Testmodus ? true : null;
        verzoek.ApplicationFee = new ApplicationFee
        {
            Amount = new Amount(Currency.EUR, Bedrag(servicekosten)),
            Description = omschrijvingKosten.Length > 255 ? omschrijvingKosten[..255] : omschrijvingKosten
        };
        return await MetTokenAsync(k, async (token, http) =>
        {
            using var client = new PaymentClient(token, http);
            return await client.CreatePaymentAsync(verzoek);
        });
    }

    public async Task<PaymentResponse?> HaalBetalingAsync(string coachId, string betalingId, bool testmodus)
    {
        var k = await KoppelingAsync(coachId);
        if (k == null || await AccessTokenAsync(k) == null) return null;
        return await MetTokenAsync(k, async (token, http) =>
        {
            using var client = new PaymentClient(token, http);
            return await client.GetPaymentAsync(betalingId, testmode: testmodus);
        });
    }

    // Voert een Mollie-aanroep uit met de token van de coach. Weigert Mollie de token (401, bijvoorbeeld
    // omdat hij eerder is ingetrokken), dan één keer opnieuw proberen met een verse token.
    private async Task<T> MetTokenAsync<T>(MollieKoppeling k, Func<string, HttpClient, Task<T>> actie)
    {
        for (var poging = 0; ; poging++)
        {
            var token = await AccessTokenAsync(k) ?? throw new InvalidOperationException("Geen toegang tot het Mollie-account van de coach.");
            using var http = _http.CreateClient(HttpNaam);
            try { return await actie(token, http); }
            catch (MollieApiException ex) when (ex.Details?.Status == 401 && poging == 0)
            {
                k.AccessTokenVersleuteld = null;
                k.AccessTokenVerlooptOp = null;
                await _context.SaveChangesAsync();
            }
        }
    }

    // ---------- Tokens ----------

    private record TokenAntwoord(string AccessToken, string? RefreshToken, int ExpiresIn);

    // Access tokens zijn een uur geldig; daarna een nieuwe ophalen met de refresh token
    private async Task<string?> AccessTokenAsync(MollieKoppeling k)
    {
        if (k.AccessTokenVersleuteld != null && k.AccessTokenVerlooptOp > DateTime.UtcNow.AddMinutes(2))
        {
            var bewaard = Ontsleutel(k.AccessTokenVersleuteld);
            if (bewaard != null) return bewaard;
        }
        var refresh = Ontsleutel(k.RefreshTokenVersleuteld);
        if (refresh == null) { await MarkeerIngetrokkenAsync(k); return null; }

        var (token, afgewezen) = await TokenAanvraagAsync(new()
        {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = refresh
        });
        if (token == null)
        {
            // Afgewezen = de coach heeft de toegang in Mollie ingetrokken; anders tijdelijk probleem
            if (afgewezen) await MarkeerIngetrokkenAsync(k);
            return null;
        }
        k.AccessTokenVersleuteld = _tokens.Protect(token.AccessToken);
        if (!string.IsNullOrEmpty(token.RefreshToken)) k.RefreshTokenVersleuteld = _tokens.Protect(token.RefreshToken);
        k.AccessTokenVerlooptOp = DateTime.UtcNow.AddSeconds(Math.Max(60, token.ExpiresIn - 60));
        if (k.OnboardingStatus == "ingetrokken") k.OnboardingStatus = null;
        await _context.SaveChangesAsync();
        return token.AccessToken;
    }

    private async Task MarkeerIngetrokkenAsync(MollieKoppeling k)
    {
        k.OnboardingStatus = "ingetrokken";
        k.KanBetalingenOntvangen = false;
        k.AccessTokenVersleuteld = null;
        k.BijgewerktOp = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    private async Task<(TokenAntwoord? token, bool afgewezen)> TokenAanvraagAsync(Dictionary<string, string> velden)
    {
        using var client = _http.CreateClient(HttpNaam);
        using var verzoek = new HttpRequestMessage(HttpMethod.Post, TokenUrl) { Content = new FormUrlEncodedContent(velden) };
        verzoek.Headers.Authorization = BasicAuth();
        using var antwoord = await client.SendAsync(verzoek);
        var tekst = await antwoord.Content.ReadAsStringAsync();
        if (!antwoord.IsSuccessStatusCode)
        {
            _logger.LogWarning("Mollie-token aanvragen mislukt ({Status}): {Tekst}", (int)antwoord.StatusCode, tekst);
            var code = (int)antwoord.StatusCode;
            return (null, code is 400 or 401 or 403);
        }
        using var doc = JsonDocument.Parse(tekst);
        var r = doc.RootElement;
        return (new TokenAntwoord(
            r.GetProperty("access_token").GetString()!,
            r.TryGetProperty("refresh_token", out var rt) ? rt.GetString() : null,
            r.TryGetProperty("expires_in", out var e) && e.TryGetInt32(out var sec) ? sec : 3600), false);
    }

    private async Task IntrekkenAsync(string refreshToken)
    {
        if (!IsIngesteld) return;
        try
        {
            using var client = _http.CreateClient(HttpNaam);
            using var verzoek = new HttpRequestMessage(HttpMethod.Delete, TokenUrl)
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["token_type_hint"] = "refresh_token",
                    ["token"] = refreshToken
                })
            };
            verzoek.Headers.Authorization = BasicAuth();
            using var antwoord = await client.SendAsync(verzoek);
            if (!antwoord.IsSuccessStatusCode)
                _logger.LogWarning("Mollie-toegang intrekken mislukt ({Status})", (int)antwoord.StatusCode);
        }
        catch (Exception ex) { _logger.LogWarning("Mollie-toegang intrekken mislukt: {Fout}", ex.Message); }
    }

    private static AuthenticationHeaderValue BasicAuth() =>
        new("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{ClientId}:{ClientSecret}")));

    private string? Ontsleutel(string? versleuteld)
    {
        if (string.IsNullOrEmpty(versleuteld)) return null;
        try { return _tokens.Unprotect(versleuteld); }
        catch { return null; } // sleutel kwijt: coach moet opnieuw koppelen
    }
}

// Lokaal testen: stuurt verkeer naar api.mollie.com / my.mollie.com naar een nep-server
public class MollieOmleiding : DelegatingHandler
{
    private readonly Uri _doel;
    public MollieOmleiding(string doel) : base(new HttpClientHandler()) => _doel = new Uri(doel);

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        if (request.RequestUri is { } u && (u.Host.EndsWith("mollie.com") || u.Host.EndsWith("mollie.nl")))
            request.RequestUri = new UriBuilder(u) { Scheme = _doel.Scheme, Host = _doel.Host, Port = _doel.Port }.Uri;
        return base.SendAsync(request, ct);
    }
}
