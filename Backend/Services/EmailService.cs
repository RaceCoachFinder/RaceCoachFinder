using System.Net.Http.Json;

namespace Backend.Services;

public interface IEmailService
{
    Task VerstuurAsync(string naarEmail, string naarNaam, string onderwerp, string htmlBody);
}

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;
    private static readonly HttpClient _http = new();

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task VerstuurAsync(string naarEmail, string naarNaam, string onderwerp, string htmlBody)
    {
        var apiKey = _config["Email:BrevoApiKey"];
        var afzender = _config["Email:Afzender"] ?? "racecoachfinder@gmail.com";
        var naamAfzender = _config["Email:NaamAfzender"] ?? "RaceCoachFinder";

        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.LogWarning("Brevo API key niet geconfigureerd.");
            return;
        }

        var payload = new
        {
            sender = new { name = naamAfzender, email = afzender },
            to = new[] { new { email = naarEmail, name = naarNaam } },
            subject = onderwerp,
            htmlContent = htmlBody
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.brevo.com/v3/smtp/email");
        request.Headers.Add("api-key", apiKey);
        request.Content = JsonContent.Create(payload);

        var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Brevo fout {(int)response.StatusCode}: {error}");
        }

        _logger.LogInformation("E-mail verstuurd via Brevo naar {Email}: {Onderwerp}", naarEmail, onderwerp);
    }
}

public static class EmailTemplates
{
    private static string Omhulsel(string inhoud)
    {
        return "<!DOCTYPE html><html lang=\"nl\"><head><meta charset=\"UTF-8\"></head>" +
               "<body style=\"margin:0;padding:0;background:#f5f5f5;font-family:Arial,sans-serif\">" +
               "<table width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"background:#f5f5f5;padding:30px 0\">" +
               "<tr><td align=\"center\">" +
               "<table width=\"560\" cellpadding=\"0\" cellspacing=\"0\" style=\"background:#fff;border-radius:10px;overflow:hidden;box-shadow:0 2px 8px rgba(0,0,0,0.08)\">" +
               "<tr><td style=\"background:#111111;padding:20px 32px;text-align:center\">" +
               "<span style=\"font-size:1.3rem;font-weight:800;color:#fff\">Race<span style=\"color:#F5C200\">Coach</span>Finder</span>" +
               "</td></tr>" +
               "<tr><td style=\"padding:32px\">" + inhoud + "</td></tr>" +
               "<tr><td style=\"background:#f9f9f9;padding:16px 32px;text-align:center;color:#999;font-size:0.78rem;border-top:1px solid #eee\">" +
               "&copy; 2026 RaceCoachFinder &mdash; Dit is een automatisch bericht." +
               "</td></tr></table></td></tr></table></body></html>";
    }

    private static string Knop(string url, string tekst)
    {
        return "<a href=\"" + url + "\" style=\"display:inline-block;background:#F5C200;color:#000;text-decoration:none;" +
               "padding:12px 28px;border-radius:6px;font-weight:700;font-size:0.95rem\">" + tekst + "</a>";
    }

    public static string NieuwBericht(string afzenderNaam, string berichtPreview, string berichtenUrl)
    {
        var inhoud =
            "<h2 style=\"margin:0 0 8px;color:#111111;font-size:1.2rem\">Je hebt een nieuw bericht!</h2>" +
            "<p style=\"color:#555;margin:0 0 20px\"><strong>" + Esc(afzenderNaam) + "</strong> heeft je een bericht gestuurd:</p>" +
            "<div style=\"background:#f5f5f5;border-left:4px solid #F5C200;padding:14px 18px;border-radius:4px;color:#333;font-style:italic;margin-bottom:24px\">" +
            "&#8220;" + Esc(berichtPreview) + "&#8221;" +
            "</div>" +
            Knop(berichtenUrl, "Bekijk bericht");
        return Omhulsel(inhoud);
    }

    public static string Welkom(string naam, string rol)
    {
        string actieKnop;
        if (rol == "Coach")
        {
            actieKnop =
                "<p style=\"color:#555;margin:0 0 24px\">Maak je profiel aan en publiceer het zodat rijders je kunnen vinden.</p>" +
                Knop("https://racecoachfinder.netlify.app/dashboard-coach.html", "Profiel aanmaken");
        }
        else
        {
            actieKnop =
                "<p style=\"color:#555;margin:0 0 24px\">Zoek een coach die bij jou past en stuur een bericht.</p>" +
                Knop("https://racecoachfinder.netlify.app/coaches.html", "Coaches bekijken");
        }

        var inhoud =
            "<h2 style=\"margin:0 0 8px;color:#111111;font-size:1.2rem\">Welkom bij RaceCoachFinder, " + Esc(naam) + "!</h2>" +
            "<p style=\"color:#555;margin:0 0 16px\">Je account als <strong>" + Esc(rol) + "</strong> is succesvol aangemaakt.</p>" +
            actieKnop;
        return Omhulsel(inhoud);
    }

    public static string WachtwoordReset(string naam, string code)
    {
        var inhoud =
            "<h2 style=\"margin:0 0 8px;color:#111111;font-size:1.2rem\">Wachtwoord resetten</h2>" +
            "<p style=\"color:#555;margin:0 0 16px\">Hallo " + Esc(naam) + ",</p>" +
            "<p style=\"color:#555;margin:0 0 24px\">Je hebt een wachtwoord reset aangevraagd. Gebruik de onderstaande code om een nieuw wachtwoord in te stellen. De code is <strong>15 minuten</strong> geldig.</p>" +
            "<div style=\"text-align:center;margin:0 0 24px\">" +
            "<div style=\"display:inline-block;background:#f9f9f9;border:2px dashed #F5C200;border-radius:10px;padding:18px 36px\">" +
            "<div style=\"font-size:0.72rem;color:#999;margin-bottom:6px;letter-spacing:1px;text-transform:uppercase\">Verificatiecode</div>" +
            "<div style=\"font-size:2.2rem;font-weight:800;letter-spacing:10px;color:#111111\">" + Esc(code) + "</div>" +
            "</div></div>" +
            "<p style=\"color:#aaa;font-size:0.82rem;margin:0\">Als je geen wachtwoord reset hebt aangevraagd, kun je deze e-mail negeren.</p>";
        return Omhulsel(inhoud);
    }

    public static string FactuurCoach(string coachNaam, string rijderNaam, string factuurnummer, string omschrijving, decimal bedrag)
    {
        var inhoud =
            "<h2 style=\"margin:0 0 8px;color:#111111;font-size:1.2rem\">Factuur verstuurd</h2>" +
            "<p style=\"color:#555;margin:0 0 16px\">Hallo " + Esc(coachNaam) + ",</p>" +
            "<p style=\"color:#555;margin:0 0 24px\">Je factuur <strong>" + Esc(factuurnummer) + "</strong> is verstuurd naar <strong>" + Esc(rijderNaam) + "</strong>.</p>" +
            "<table width=\"100%\" cellpadding=\"10\" cellspacing=\"0\" style=\"border:1px solid #eee;border-radius:8px;margin-bottom:24px\">" +
            "<tr><td style=\"color:#999;font-size:0.82rem\">Omschrijving</td><td style=\"color:#999;font-size:0.82rem;text-align:right\">Bedrag</td></tr>" +
            "<tr><td style=\"color:#111;font-weight:600\">" + Esc(omschrijving) + "</td>" +
            "<td style=\"color:#111;font-weight:700;text-align:right\">€ " + bedrag.ToString("F2").Replace(".", ",") + "</td></tr>" +
            "</table>" +
            "<p style=\"color:#aaa;font-size:0.82rem;margin:0\">De rijder ontvangt een e-mail met betaalinstructies.</p>";
        return Omhulsel(inhoud);
    }

    public static string FactuurRijder(string rijderNaam, string coachNaam, string factuurnummer, string omschrijving, decimal bedrag, int termijn, string berichtenUrl)
    {
        var vervaldatum = DateTime.Now.AddDays(termijn).ToString("d MMMM yyyy", new System.Globalization.CultureInfo("nl-NL"));
        var inhoud =
            "<h2 style=\"margin:0 0 8px;color:#111111;font-size:1.2rem\">Factuur van " + Esc(coachNaam) + "</h2>" +
            "<p style=\"color:#555;margin:0 0 16px\">Hallo " + Esc(rijderNaam) + ",</p>" +
            "<p style=\"color:#555;margin:0 0 20px\">Je hebt een factuur ontvangen van coach <strong>" + Esc(coachNaam) + "</strong>.</p>" +
            "<table width=\"100%\" cellpadding=\"10\" cellspacing=\"0\" style=\"border:1px solid #eee;border-radius:8px;margin-bottom:8px\">" +
            "<tr style=\"background:#f9f9f9\"><td style=\"color:#999;font-size:0.82rem;border-bottom:1px solid #eee\">Factuurnummer</td>" +
            "<td style=\"color:#999;font-size:0.82rem;border-bottom:1px solid #eee;text-align:right\">" + Esc(factuurnummer) + "</td></tr>" +
            "<tr><td style=\"color:#111;font-weight:600\">" + Esc(omschrijving) + "</td>" +
            "<td style=\"color:#111;font-weight:700;text-align:right\">€ " + bedrag.ToString("F2").Replace(".", ",") + "</td></tr>" +
            "</table>" +
            "<p style=\"color:#e65100;font-size:0.85rem;margin:0 0 24px\">Betalen vóór: <strong>" + vervaldatum + "</strong></p>" +
            Knop(berichtenUrl, "Betaal nu via RaceCoachFinder") +
            "<p style=\"color:#aaa;font-size:0.78rem;margin:24px 0 0\">Klik op de knop om veilig te betalen via onze website.</p>";
        return Omhulsel(inhoud);
    }

    private static string Esc(string s) =>
        s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
}
