using System.Net.Http.Json;

namespace Backend.Services;

public interface IEmailService
{
    Task VerstuurAsync(string naarEmail, string naarNaam, string onderwerp, string htmlBody, byte[]? pdfBijlage = null, string? bijlageNaam = null);
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

    public async Task VerstuurAsync(string naarEmail, string naarNaam, string onderwerp, string htmlBody, byte[]? pdfBijlage = null, string? bijlageNaam = null)
    {
        var apiKey = _config["Email:BrevoApiKey"];
        var afzender = _config["Email:Afzender"] ?? "racecoachfinder@gmail.com";
        var naamAfzender = _config["Email:NaamAfzender"] ?? "RaceCoachFinder";

        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.LogWarning("Brevo API key niet geconfigureerd.");
            return;
        }

        object payload;
        if (pdfBijlage != null && pdfBijlage.Length > 0)
        {
            payload = new
            {
                sender = new { name = naamAfzender, email = afzender },
                to = new[] { new { email = naarEmail, name = naarNaam } },
                subject = onderwerp,
                htmlContent = htmlBody,
                attachment = new[] { new { content = Convert.ToBase64String(pdfBijlage), name = bijlageNaam ?? "factuur.pdf" } }
            };
        }
        else
        {
            payload = new
            {
                sender = new { name = naamAfzender, email = afzender },
                to = new[] { new { email = naarEmail, name = naarNaam } },
                subject = onderwerp,
                htmlContent = htmlBody
            };
        }

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

// Taal van de e-mails van een gebruiker (instelling; standaard Nederlands)
public static class EmailTaal
{
    public static async Task<string> VoorAsync(Backend.Data.AppDbContext context, string gebruikerId)
    {
        var taal = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(
            context.GebruikerInstellingen.Where(i => i.GebruikerId == gebruikerId).Select(i => i.EmailTaal));
        return taal == "en" ? "en" : "nl";
    }
}

// Alle e-mails in het Nederlands ('nl') of Engels ('en')
public static class EmailTemplates
{
    private static bool En(string taal) => taal == "en";
    private static string T(string taal, string nl, string en) => En(taal) ? en : nl;

    private static string Omhulsel(string inhoud, string taal = "nl")
    {
        return "<!DOCTYPE html><html lang=\"" + (En(taal) ? "en" : "nl") + "\"><head><meta charset=\"UTF-8\"></head>" +
               "<body style=\"margin:0;padding:0;background:#f5f5f5;font-family:Arial,sans-serif\">" +
               "<table width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"background:#f5f5f5;padding:30px 0\">" +
               "<tr><td align=\"center\">" +
               "<table width=\"560\" cellpadding=\"0\" cellspacing=\"0\" style=\"background:#fff;border-radius:10px;overflow:hidden;box-shadow:0 2px 8px rgba(0,0,0,0.08)\">" +
               "<tr><td style=\"background:#111111;padding:20px 32px;text-align:center\">" +
               "<span style=\"font-size:1.3rem;font-weight:800;color:#fff\">Race<span style=\"color:#F5C200\">Coach</span>Finder</span>" +
               "</td></tr>" +
               "<tr><td style=\"padding:32px\">" + inhoud + "</td></tr>" +
               "<tr><td style=\"background:#f9f9f9;padding:16px 32px;text-align:center;color:#999;font-size:0.78rem;border-top:1px solid #eee\">" +
               "&copy; 2026 RaceCoachFinder &mdash; " + T(taal, "Dit is een automatisch bericht.", "This is an automated message.") +
               " <a href=\"https://www.racecoachfinder.nl/instellingen.html\" style=\"color:#999\">" + T(taal, "E-mailinstellingen", "Email settings") + "</a>" +
               "</td></tr></table></td></tr></table></body></html>";
    }

    private static string Knop(string url, string tekst)
    {
        return "<a href=\"" + url + "\" style=\"display:inline-block;background:#F5C200;color:#000;text-decoration:none;" +
               "padding:12px 28px;border-radius:6px;font-weight:700;font-size:0.95rem\">" + tekst + "</a>";
    }

    private static string Voornaam(string? naam) =>
        (naam ?? "").Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";

    private static string Bedrag(decimal bedrag, string taal) =>
        "€ " + bedrag.ToString("0.00", new System.Globalization.CultureInfo(En(taal) ? "en-GB" : "nl-NL"));

    private static string CodeBlok(string code, string taal) =>
        "<div style=\"text-align:center;margin:0 0 24px\">" +
        "<div style=\"display:inline-block;background:#f9f9f9;border:2px dashed #F5C200;border-radius:10px;padding:18px 36px\">" +
        "<div style=\"font-size:0.72rem;color:#999;margin-bottom:6px;letter-spacing:1px;text-transform:uppercase\">" + T(taal, "Verificatiecode", "Verification code") + "</div>" +
        "<div style=\"font-size:2.2rem;font-weight:800;letter-spacing:10px;color:#111111\">" + Esc(code) + "</div>" +
        "</div></div>";

    // Datum zoals in Nederland ("9 maart 2027") of in het Engels ("9 March 2027"), in Nederlandse tijd
    public static string Datum(DateTime utc, string taal = "nl")
    {
        var tijd = DateTime.SpecifyKind(utc, DateTimeKind.Utc);
        try { tijd = TimeZoneInfo.ConvertTimeFromUtc(tijd, TimeZoneInfo.FindSystemTimeZoneById("Europe/Amsterdam")); }
        catch { }
        return tijd.ToString("d MMMM yyyy", new System.Globalization.CultureInfo(En(taal) ? "en-GB" : "nl-NL"));
    }
    public static string NlDatum(DateTime utc) => Datum(utc, "nl");

    // ── Berichten ──
    public static string NieuwBerichtOnderwerp(string afzender, string taal = "nl") =>
        T(taal, $"Nieuw bericht van {afzender} – RaceCoachFinder", $"New message from {afzender} – RaceCoachFinder");

    public static string NieuwBericht(string afzenderNaam, string berichtPreview, string berichtenUrl, string taal = "nl")
    {
        var inhoud =
            "<h2 style=\"margin:0 0 8px;color:#111111;font-size:1.2rem\">" + T(taal, "Je hebt een nieuw bericht!", "You have a new message!") + "</h2>" +
            "<p style=\"color:#555;margin:0 0 20px\"><strong>" + Esc(afzenderNaam) + "</strong> " + T(taal, "heeft je een bericht gestuurd:", "sent you a message:") + "</p>" +
            "<div style=\"background:#f5f5f5;border-left:4px solid #F5C200;padding:14px 18px;border-radius:4px;color:#333;font-style:italic;margin-bottom:24px\">" +
            "&#8220;" + Esc(berichtPreview) + "&#8221;" +
            "</div>" +
            Knop(berichtenUrl, T(taal, "Bekijk bericht", "View message"));
        return Omhulsel(inhoud, taal);
    }

    // ── Account ──
    public static string WelkomOnderwerp(string taal = "nl") => T(taal, "Welkom bij RaceCoachFinder!", "Welcome to RaceCoachFinder!");

    public static string Welkom(string naam, string rol, string taal = "nl")
    {
        var isCoach = (rol ?? "").Split(',').Any(r => r.Trim() == "Coach");
        var actieKnop = isCoach
            ? "<p style=\"color:#555;margin:0 0 24px\">" + T(taal, "Maak je profiel aan en publiceer het zodat rijders je kunnen vinden.", "Create your profile and publish it so riders can find you.") + "</p>" +
              Knop("https://www.racecoachfinder.nl/dashboard-coach.html", T(taal, "Profiel aanmaken", "Create profile"))
            : "<p style=\"color:#555;margin:0 0 24px\">" + T(taal, "Zoek een coach die bij jou past en stuur een bericht.", "Find a coach that suits you and send a message.") + "</p>" +
              Knop("https://www.racecoachfinder.nl/coaches.html", T(taal, "Coaches bekijken", "View coaches"));
        var rolTekst = En(taal) ? (rol ?? "").Replace("Rijder", "Rider").Replace(",", " & ") : (rol ?? "").Replace(",", " & ");
        var inhoud =
            "<h2 style=\"margin:0 0 8px;color:#111111;font-size:1.2rem\">" + T(taal, "Welkom bij RaceCoachFinder, ", "Welcome to RaceCoachFinder, ") + Esc(naam) + "!</h2>" +
            "<p style=\"color:#555;margin:0 0 16px\">" + T(taal, "Je account als <strong>" + Esc(rolTekst) + "</strong> is succesvol aangemaakt.", "Your <strong>" + Esc(rolTekst) + "</strong> account has been created.") + "</p>" +
            actieKnop;
        return Omhulsel(inhoud, taal);
    }

    public static string WachtwoordResetOnderwerp(string taal = "nl") => T(taal, "Wachtwoord resetten – RaceCoachFinder", "Reset your password – RaceCoachFinder");

    public static string WachtwoordReset(string naam, string code, string taal = "nl")
    {
        var inhoud =
            "<h2 style=\"margin:0 0 8px;color:#111111;font-size:1.2rem\">" + T(taal, "Wachtwoord resetten", "Reset your password") + "</h2>" +
            "<p style=\"color:#555;margin:0 0 16px\">" + T(taal, "Hallo ", "Hi ") + Esc(naam) + ",</p>" +
            "<p style=\"color:#555;margin:0 0 24px\">" + T(taal,
                "Je hebt een wachtwoord reset aangevraagd. Gebruik de onderstaande code om een nieuw wachtwoord in te stellen. De code is <strong>15 minuten</strong> geldig.",
                "You requested a password reset. Use the code below to set a new password. The code is valid for <strong>15 minutes</strong>.") + "</p>" +
            CodeBlok(code, taal) +
            "<p style=\"color:#aaa;font-size:0.82rem;margin:0\">" + T(taal, "Als je geen wachtwoord reset hebt aangevraagd, kun je deze e-mail negeren.", "If you did not request a password reset, you can ignore this email.") + "</p>";
        return Omhulsel(inhoud, taal);
    }

    public static string EmailBevestigingOnderwerp(string taal = "nl") => T(taal, "Bevestig je e-mailadres – RaceCoachFinder", "Confirm your email address – RaceCoachFinder");

    public static string EmailBevestiging(string naam, string code, string taal = "nl")
    {
        var inhoud =
            "<h2 style=\"margin:0 0 8px;color:#111111;font-size:1.2rem\">" + T(taal, "Bevestig je e-mailadres", "Confirm your email address") + "</h2>" +
            "<p style=\"color:#555;margin:0 0 16px\">" + T(taal, "Hallo ", "Hi ") + Esc(naam) + ",</p>" +
            "<p style=\"color:#555;margin:0 0 24px\">" + T(taal,
                "Voer de onderstaande code in om je account te activeren. De code is <strong>30 minuten</strong> geldig.",
                "Enter the code below to activate your account. The code is valid for <strong>30 minutes</strong>.") + "</p>" +
            CodeBlok(code, taal) +
            "<p style=\"color:#aaa;font-size:0.82rem;margin:0\">" + T(taal, "Als je dit account niet zelf hebt aangemaakt, kun je deze e-mail negeren.", "If you did not create this account, you can ignore this email.") + "</p>";
        return Omhulsel(inhoud, taal);
    }

    // ── Facturen ──
    public static string FactuurCoachOnderwerp(string nr, string taal = "nl") => T(taal, $"Factuur {nr} verstuurd", $"Invoice {nr} sent");

    public static string FactuurCoach(string coachNaam, string rijderNaam, string factuurnummer, string omschrijving, decimal bedrag, string taal = "nl")
    {
        var inhoud =
            "<h2 style=\"margin:0 0 8px;color:#111111;font-size:1.2rem\">" + T(taal, "Factuur verstuurd", "Invoice sent") + "</h2>" +
            "<p style=\"color:#555;margin:0 0 16px\">" + T(taal, "Hallo ", "Hi ") + Esc(coachNaam) + ",</p>" +
            "<p style=\"color:#555;margin:0 0 24px\">" + T(taal,
                "Je factuur <strong>" + Esc(factuurnummer) + "</strong> is verstuurd naar <strong>" + Esc(rijderNaam) + "</strong>.",
                "Your invoice <strong>" + Esc(factuurnummer) + "</strong> has been sent to <strong>" + Esc(rijderNaam) + "</strong>.") + "</p>" +
            "<table width=\"100%\" cellpadding=\"10\" cellspacing=\"0\" style=\"border:1px solid #eee;border-radius:8px;margin-bottom:24px\">" +
            "<tr><td style=\"color:#999;font-size:0.82rem\">" + T(taal, "Omschrijving", "Description") + "</td><td style=\"color:#999;font-size:0.82rem;text-align:right\">" + T(taal, "Bedrag", "Amount") + "</td></tr>" +
            "<tr><td style=\"color:#111;font-weight:600\">" + Esc(omschrijving) + "</td>" +
            "<td style=\"color:#111;font-weight:700;text-align:right\">" + Bedrag(bedrag, taal) + "</td></tr>" +
            "</table>" +
            "<p style=\"color:#aaa;font-size:0.82rem;margin:0\">" + T(taal, "De rijder ontvangt een e-mail met betaalinstructies.", "The rider receives an email with payment instructions.") + "</p>";
        return Omhulsel(inhoud, taal);
    }

    public static string FactuurRijderOnderwerp(string nr, string coach, string taal = "nl") => T(taal, $"Factuur {nr} van {coach}", $"Invoice {nr} from {coach}");

    public static string FactuurRijder(string rijderNaam, string coachNaam, string factuurnummer, string omschrijving, decimal bedrag, int termijn, string berichtenUrl, string taal = "nl",
        string? iban = null, string? tenaamstelling = null, string? betaalLink = null)
    {
        var vervaldatum = Datum(DateTime.UtcNow.AddDays(termijn), taal);
        var inhoud =
            "<h2 style=\"margin:0 0 8px;color:#111111;font-size:1.2rem\">" + T(taal, "Factuur van ", "Invoice from ") + Esc(coachNaam) + "</h2>" +
            "<p style=\"color:#555;margin:0 0 16px\">" + T(taal, "Hallo ", "Hi ") + Esc(rijderNaam) + ",</p>" +
            "<p style=\"color:#555;margin:0 0 20px\">" + T(taal, "Je hebt een factuur ontvangen van coach <strong>", "You have received an invoice from coach <strong>") + Esc(coachNaam) + "</strong>.</p>" +
            "<table width=\"100%\" cellpadding=\"10\" cellspacing=\"0\" style=\"border:1px solid #eee;border-radius:8px;margin-bottom:8px\">" +
            "<tr style=\"background:#f9f9f9\"><td style=\"color:#999;font-size:0.82rem;border-bottom:1px solid #eee\">" + T(taal, "Factuurnummer", "Invoice number") + "</td>" +
            "<td style=\"color:#999;font-size:0.82rem;border-bottom:1px solid #eee;text-align:right\">" + Esc(factuurnummer) + "</td></tr>" +
            "<tr><td style=\"color:#111;font-weight:600\">" + Esc(omschrijving) + "</td>" +
            "<td style=\"color:#111;font-weight:700;text-align:right\">" + Bedrag(bedrag, taal) + "</td></tr>" +
            "</table>" +
            "<p style=\"color:#e65100;font-size:0.85rem;margin:0 0 24px\">" + T(taal, "Betalen vóór: ", "Pay before: ") + "<strong>" + vervaldatum + "</strong></p>";
        if (!string.IsNullOrEmpty(iban))
        {
            // Coach zonder Mollie: de rijder maakt het bedrag zelf over
            inhoud +=
                "<div style=\"background:#f7f7f9;border-radius:8px;padding:14px 16px;margin:0 0 20px;color:#333;font-size:0.9rem;line-height:1.6\">" +
                T(taal, "Maak het bedrag over naar:", "Please transfer the amount to:") + "<br>" +
                "<strong>" + Esc(iban) + "</strong>" + (string.IsNullOrEmpty(tenaamstelling) ? "" : T(taal, " t.n.v. ", " in the name of ") + "<strong>" + Esc(tenaamstelling) + "</strong>") + "<br>" +
                T(taal, "Onder vermelding van ", "Reference: ") + "<strong>" + Esc(factuurnummer) + "</strong></div>" +
                (string.IsNullOrEmpty(betaalLink) ? "" : Knop(betaalLink, T(taal, "Betaal via betaalverzoek", "Pay via payment request")) + "<br>") +
                "<p style=\"color:#555;font-size:0.85rem;margin:16px 0 0\">" + T(taal,
                    "Heb je betaald? Klik dan in de chat bij de factuur op <strong>Ik heb betaald</strong>, dan krijgt je coach een seintje.",
                    "Paid? Click <strong>I have paid</strong> next to the invoice in the chat, so your coach gets a notification.") + "</p>" +
                "<p style=\"margin:16px 0 0\"><a href=\"" + berichtenUrl + "\" style=\"color:#1565c0\">" + T(taal, "Bekijk de factuur in je berichten", "View the invoice in your messages") + "</a></p>";
        }
        else
        {
            inhoud +=
                Knop(berichtenUrl, T(taal, "Betaal nu via RaceCoachFinder", "Pay now via RaceCoachFinder")) +
                "<p style=\"color:#aaa;font-size:0.78rem;margin:24px 0 0\">" + T(taal, "Klik op de knop om veilig te betalen via onze website.", "Click the button to pay securely via our website.") + "</p>";
        }
        return Omhulsel(inhoud, taal);
    }

    // Overmaken: rijder meldt dat hij betaald heeft (naar de coach)
    public static string BetalingGemeldOnderwerp(string nr, string rijder, string taal = "nl") =>
        T(taal, $"{rijder} heeft factuur {nr} betaald", $"{rijder} has paid invoice {nr}");

    public static string BetalingGemeld(string coachNaam, string rijderNaam, string factuurnummer, decimal bedrag, string berichtenUrl, string taal = "nl")
    {
        var inhoud =
            "<h2 style=\"margin:0 0 8px;color:#111111;font-size:1.2rem\">" + T(taal, "Betaling gemeld 💶", "Payment reported 💶") + "</h2>" +
            "<p style=\"color:#555;margin:0 0 16px\">" + T(taal, "Hoi ", "Hi ") + Esc(Voornaam(coachNaam)) + ", <strong>" + Esc(rijderNaam) + "</strong> " +
            T(taal, "geeft aan dat het bedrag voor factuur <strong>" + Esc(factuurnummer) + "</strong> (" + Bedrag(bedrag, taal) + ") is overgemaakt.",
                    "says they have transferred the amount for invoice <strong>" + Esc(factuurnummer) + "</strong> (" + Bedrag(bedrag, taal) + ").") + "</p>" +
            "<p style=\"color:#555;margin:0 0 20px\">" + T(taal,
                "Controleer of het geld op je rekening staat en klik dan in de chat op <strong>Ontvangen</strong>.",
                "Check that the money is in your account, then click <strong>Received</strong> in the chat.") + "</p>" +
            Knop(berichtenUrl, T(taal, "Naar de factuur", "Go to the invoice"));
        return Omhulsel(inhoud, taal);
    }

    // Overmaken: coach bevestigt dat het geld binnen is (naar de rijder)
    public static string BetalingOntvangenOnderwerp(string nr, string coach, string taal = "nl") =>
        T(taal, $"{coach} heeft je betaling ontvangen ({nr})", $"{coach} has received your payment ({nr})");

    public static string BetalingOntvangen(string rijderNaam, string coachNaam, string factuurnummer, decimal bedrag, string berichtenUrl, string taal = "nl")
    {
        var inhoud =
            "<h2 style=\"margin:0 0 8px;color:#111111;font-size:1.2rem\">" + T(taal, "Betaling ontvangen ✅", "Payment received ✅") + "</h2>" +
            "<p style=\"color:#555;margin:0 0 16px\">" + T(taal, "Hoi ", "Hi ") + Esc(Voornaam(rijderNaam)) + ", <strong>" + Esc(coachNaam) + "</strong> " +
            T(taal, "heeft je betaling voor factuur <strong>" + Esc(factuurnummer) + "</strong> (" + Bedrag(bedrag, taal) + ") ontvangen. Bedankt!",
                    "has received your payment for invoice <strong>" + Esc(factuurnummer) + "</strong> (" + Bedrag(bedrag, taal) + "). Thank you!") + "</p>" +
            Knop(berichtenUrl, T(taal, "Naar je berichten", "Go to your messages"));
        return Omhulsel(inhoud, taal);
    }

    public static string FactuurBetaaldOnderwerp(string nr, string taal = "nl") => T(taal, $"Factuur {nr} is betaald – RaceCoachFinder", $"Invoice {nr} has been paid – RaceCoachFinder");

    public static string FactuurBetaald(string coachNaam, string rijderNaam, string factuurnummer, string omschrijving, decimal bedrag, string berichtenUrl, string taal = "nl")
    {
        var inhoud =
            "<h2 style=\"margin:0 0 8px;color:#111111;font-size:1.2rem\">" + T(taal, "Factuur betaald ✅", "Invoice paid ✅") + "</h2>" +
            "<p style=\"color:#555;margin:0 0 16px\">" + T(taal, "Hoi ", "Hi ") + Esc(Voornaam(coachNaam)) + ", <strong>" + Esc(rijderNaam) + "</strong> " +
            T(taal, "heeft factuur <strong>" + Esc(factuurnummer) + "</strong> betaald.", "has paid invoice <strong>" + Esc(factuurnummer) + "</strong>.") + "</p>" +
            "<table style=\"width:100%;border-collapse:collapse;margin-bottom:20px\">" +
            "<tr><td style=\"padding:8px 12px;background:#f9f9f9;font-weight:700;width:130px;border:1px solid #eee\">" + T(taal, "Omschrijving", "Description") + "</td>" +
            "<td style=\"padding:8px 12px;border:1px solid #eee\">" + Esc(omschrijving) + "</td></tr>" +
            "<tr><td style=\"padding:8px 12px;background:#f9f9f9;font-weight:700;border:1px solid #eee\">" + T(taal, "Bedrag", "Amount") + "</td>" +
            "<td style=\"padding:8px 12px;border:1px solid #eee\">" + Bedrag(bedrag, taal) + "</td></tr>" +
            "</table>" +
            "<p style=\"color:#555;margin:0 0 24px\">" + T(taal, "Na de activiteit bevestigen jullie allebei in het gesprek dat de activiteit gedaan is.", "After the activity you both confirm in the chat that it took place.") + "</p>" +
            "<div style=\"text-align:center\">" + Knop(berichtenUrl, T(taal, "Naar het gesprek", "Go to the chat")) + "</div>";
        return Omhulsel(inhoud, taal);
    }

    // ── Agenda ──
    public static string AgendaReminderOnderwerp(DateTime datum, string taal = "nl") =>
        T(taal, "Agenda herinnering – ", "Calendar reminder – ") + datum.ToString("d MMMM", new System.Globalization.CultureInfo(En(taal) ? "en-GB" : "nl-NL"));

    public static string AgendaReminder(string naam, DateTime datum, List<Backend.Models.AgendaItem> items, string taal = "nl")
    {
        var datumTekst = datum.ToString("dddd d MMMM yyyy", new System.Globalization.CultureInfo(En(taal) ? "en-GB" : "nl-NL"));
        var itemsHtml = string.Join("", items.Select(i =>
        {
            var tijdTekst = i.Datum.ToString("HH:mm");
            var notitie = string.IsNullOrWhiteSpace(i.Notitie)
                ? ""
                : $"<div style=\"font-size:0.82rem;color:#888;margin-top:2px\">{Esc(i.Notitie)}</div>";
            return $"<tr><td style=\"padding:10px 12px;border-bottom:1px solid #f0f0f0\">" +
                   $"<span style=\"font-weight:700;color:#111\">{Esc(i.Titel)}</span>" +
                   $"<span style=\"color:#888;font-size:0.85rem;margin-left:8px\">{tijdTekst}</span>" +
                   notitie +
                   "</td></tr>";
        }));

        var inhoud =
            "<h2 style=\"margin:0 0 8px;color:#111111;font-size:1.2rem\">" + T(taal, "Agenda herinnering", "Calendar reminder") + "</h2>" +
            "<p style=\"color:#555;margin:0 0 16px\">" + T(taal, "Hallo ", "Hi ") + Esc(naam) + ",</p>" +
            "<p style=\"color:#555;margin:0 0 20px\">" + T(taal, "Op <strong>" + datumTekst + "</strong> staan de volgende items in je agenda:", "On <strong>" + datumTekst + "</strong> you have the following in your calendar:") + "</p>" +
            "<table width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"border:1px solid #eee;border-radius:8px;overflow:hidden;margin-bottom:24px\">" +
            itemsHtml +
            "</table>" +
            Knop("https://www.racecoachfinder.nl/dashboard-coach.html", T(taal, "Naar agenda", "Open calendar")) +
            "<p style=\"color:#aaa;font-size:0.78rem;margin:20px 0 0\">" + T(taal,
                "Je ontvangt deze herinnering omdat je agenda-reminders hebt ingeschakeld. Pas dit aan via ",
                "You receive this reminder because calendar reminders are turned on. Change this in ") +
            "<a href=\"https://www.racecoachfinder.nl/instellingen.html\" style=\"color:#888\">" + T(taal, "Instellingen", "Settings") + "</a>.</p>";
        return Omhulsel(inhoud, taal);
    }

    // ── Voor de beheerder (altijd Nederlands) ──
    public static string CoachRapport(string coachNaam, int coachId, string reden, string melderInfo, string coachUrl)
    {
        var inhoud =
            "<h2 style=\"margin:0 0 8px;color:#c62828;font-size:1.2rem\">⚑ Nieuwe melding over een coach</h2>" +
            "<p style=\"color:#555;margin:0 0 20px\">Er is een melding ingediend via de coachpagina.</p>" +
            "<table style=\"width:100%;border-collapse:collapse;margin-bottom:20px\">" +
            "<tr><td style=\"padding:8px 12px;background:#f9f9f9;font-weight:700;width:130px;border:1px solid #eee\">Coach</td>" +
            "<td style=\"padding:8px 12px;border:1px solid #eee\">" + Esc(coachNaam) + " (ID: " + coachId + ")</td></tr>" +
            "<tr><td style=\"padding:8px 12px;background:#f9f9f9;font-weight:700;border:1px solid #eee\">Melder</td>" +
            "<td style=\"padding:8px 12px;border:1px solid #eee\">" + Esc(melderInfo) + "</td></tr>" +
            "<tr><td style=\"padding:8px 12px;background:#f9f9f9;font-weight:700;border:1px solid #eee\">Reden</td>" +
            "<td style=\"padding:8px 12px;border:1px solid #eee;white-space:pre-wrap\">" + Esc(reden) + "</td></tr>" +
            "</table>" +
            "<div style=\"text-align:center\">" + Knop(coachUrl, "Bekijk coachpagina") + "</div>";
        return Omhulsel(inhoud);
    }

    // ── Reviews ──
    public static string ReviewOntvangenOnderwerp(string rijder, string taal = "nl") => T(taal, $"Nieuwe review van {rijder} – RaceCoachFinder", $"New review from {rijder} – RaceCoachFinder");

    public static string ReviewOntvangen(string coachNaam, string rijderNaam, int sterren, string tekst, string profielUrl, string taal = "nl")
    {
        var ster = new string('★', Math.Clamp(sterren, 0, 5)) + new string('☆', 5 - Math.Clamp(sterren, 0, 5));
        var inhoud =
            "<h2 style=\"margin:0 0 8px;color:#111111;font-size:1.2rem\">" + T(taal, "Je hebt een nieuwe review!", "You have a new review!") + "</h2>" +
            "<p style=\"color:#555;margin:0 0 16px\">" + T(taal, "Hoi ", "Hi ") + Esc(Voornaam(coachNaam)) + ", <strong>" + Esc(rijderNaam) + "</strong> " +
            T(taal, "heeft een review op je profiel geplaatst:", "posted a review on your profile:") + "</p>" +
            "<div style=\"font-size:1.4rem;color:#F5C200;letter-spacing:2px;margin:0 0 8px\">" + ster + "</div>" +
            (string.IsNullOrWhiteSpace(tekst) ? "" :
            "<div style=\"background:#f5f5f5;border-left:4px solid #F5C200;padding:14px 18px;border-radius:4px;color:#333;font-style:italic;margin-bottom:24px\">&#8220;" + Esc(tekst) + "&#8221;</div>") +
            "<div style=\"text-align:center;margin-top:16px\">" + Knop(profielUrl, T(taal, "Bekijk je profiel", "View your profile")) + "</div>";
        return Omhulsel(inhoud, taal);
    }

    // ── Gratis periode ──
    public static string GratisPeriodeTitel(DateTime verlooptOp, bool verlengd, string taal = "nl") =>
        verlooptOp.Year >= 9999 ? T(taal, "Je account is voor altijd gratis", "Your account is free forever")
        : verlengd ? T(taal, "Je gratis periode is verlengd", "Your free period has been extended")
        : T(taal, "Je hebt een gratis periode gekregen", "You have received a free period");

    public static string GratisPeriodeKort(DateTime verlooptOp, string taal = "nl") =>
        verlooptOp.Year >= 9999
            ? T(taal, "Je kunt RaceCoachFinder als coach voor altijd gratis gebruiken.", "You can use RaceCoachFinder as a coach for free, forever.")
            : T(taal, $"Je kunt RaceCoachFinder als coach gratis gebruiken t/m {Datum(verlooptOp, taal)}.", $"You can use RaceCoachFinder as a coach for free until {Datum(verlooptOp, taal)}.");

    public static string GratisPeriode(string naam, DateTime verlooptOp, bool verlengd, string taal = "nl")
    {
        var altijd = verlooptOp.Year >= 9999;
        var uitleg = altijd
            ? T(taal, "Goed nieuws: je kunt RaceCoachFinder als coach <strong>voor altijd gratis</strong> gebruiken. Je hebt geen abonnement nodig.",
                      "Good news: you can use RaceCoachFinder as a coach <strong>for free, forever</strong>. You do not need a subscription.")
            : (verlengd ? T(taal, "Goed nieuws: je gratis periode bij RaceCoachFinder is verlengd. ", "Good news: your free period at RaceCoachFinder has been extended. ")
                        : T(taal, "Goed nieuws: je hebt van RaceCoachFinder een gratis periode gekregen. ", "Good news: RaceCoachFinder has given you a free period. ")) +
              T(taal, "Je kunt RaceCoachFinder als coach gratis gebruiken tot en met <strong>", "You can use RaceCoachFinder as a coach for free until <strong>") + Datum(verlooptOp, taal) + "</strong>.";
        var inhoud =
            "<div style=\"font-size:2.4rem;text-align:center;margin:0 0 8px\">🎁</div>" +
            "<h2 style=\"margin:0 0 16px;color:#111111;font-size:1.2rem;text-align:center\">" + GratisPeriodeTitel(verlooptOp, verlengd, taal) + "!</h2>" +
            "<p style=\"color:#555;margin:0 0 12px\">" + T(taal, "Hoi ", "Hi ") + Esc(Voornaam(naam)) + ",</p>" +
            "<p style=\"color:#555;margin:0 0 12px\">" + uitleg + "</p>" +
            "<p style=\"color:#555;margin:0 0 24px\">" + T(taal,
                "Je hoeft hiervoor niets te doen: je profiel blijft gewoon zichtbaar voor rijders. In je dashboard zie je altijd hoe lang je gratis periode nog loopt.",
                "You do not need to do anything: your profile stays visible to riders. Your dashboard always shows how long your free period lasts.") + "</p>" +
            "<div style=\"text-align:center\">" + Knop("https://www.racecoachfinder.nl/dashboard-coach.html", T(taal, "Naar mijn dashboard", "Go to my dashboard")) + "</div>";
        return Omhulsel(inhoud, taal);
    }

    // ── Wachtlijst: plek vrij bij een volle training ──
    public static string PlekVrijOnderwerp(string titel, string taal = "nl") => T(taal, $"Er is een plek vrij: {titel}", $"A spot is available: {titel}");

    public static string PlekVrij(string naam, string titel, DateTime datum, string locatie, string url, string taal = "nl")
    {
        var inhoud =
            "<h2 style=\"margin:0 0 8px;color:#111111;font-size:1.2rem\">" + T(taal, "Er is een plek vrijgekomen! 🏁", "A spot has become available! 🏁") + "</h2>" +
            "<p style=\"color:#555;margin:0 0 16px\">" + T(taal, "Hoi ", "Hi ") + Esc(Voornaam(naam)) + ",</p>" +
            "<p style=\"color:#555;margin:0 0 16px\">" + T(taal,
                "Bij <strong>" + Esc(titel) + "</strong> (" + Esc(locatie) + ", " + Datum(datum, taal) + ") is weer een plek vrij. Je stond op de lijst voor een melding.",
                "A spot has opened up for <strong>" + Esc(titel) + "</strong> (" + Esc(locatie) + ", " + Datum(datum, taal) + "). You asked to be notified.") + "</p>" +
            "<p style=\"color:#555;margin:0 0 24px\">" + T(taal, "Wie het eerst reageert, is het eerst aan de beurt.", "First come, first served.") + "</p>" +
            "<div style=\"text-align:center\">" + Knop(url, T(taal, "Bekijk de training", "View the training")) + "</div>";
        return Omhulsel(inhoud, taal);
    }

    private static string Esc(string s) =>
        (s ?? "").Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
}
