using System.Net;
using System.Net.Mail;

namespace Backend.Services;

public interface IEmailService
{
    Task VerstuurAsync(string naarEmail, string naarNaam, string onderwerp, string htmlBody);
}

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task VerstuurAsync(string naarEmail, string naarNaam, string onderwerp, string htmlBody)
    {
        var host = _config["Email:Host"];
        var afzender = _config["Email:Afzender"];
        var wachtwoord = _config["Email:Wachtwoord"];

        if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(afzender) ||
            string.IsNullOrEmpty(wachtwoord) || afzender.StartsWith("JOUW"))
        {
            _logger.LogWarning("E-mail niet verzonden: vul Email:Afzender en Email:Wachtwoord in in appsettings.json.");
            return;
        }

        var port = int.Parse(_config["Email:Port"] ?? "587");
        var naamAfzender = _config["Email:NaamAfzender"] ?? "RaceCoachFinder";

        try
        {
#pragma warning disable SYSLIB0006
            using var smtp = new SmtpClient(host, port)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(afzender, wachtwoord)
            };

            var mail = new MailMessage
            {
                From = new MailAddress(afzender, naamAfzender),
                Subject = onderwerp,
                Body = htmlBody,
                IsBodyHtml = true
            };
            mail.To.Add(new MailAddress(naarEmail, naarNaam));

            await smtp.SendMailAsync(mail);
            _logger.LogInformation("E-mail verstuurd naar {Email}: {Onderwerp}", naarEmail, onderwerp);
#pragma warning restore SYSLIB0006
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fout bij verzenden e-mail naar {Email}", naarEmail);
        }
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
                Knop("http://localhost:5500/dashboard-coach.html", "Profiel aanmaken");
        }
        else
        {
            actieKnop =
                "<p style=\"color:#555;margin:0 0 24px\">Zoek een coach die bij jou past en stuur een bericht.</p>" +
                Knop("http://localhost:5500/coaches.html", "Coaches bekijken");
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

    private static string Esc(string s) =>
        s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
}
