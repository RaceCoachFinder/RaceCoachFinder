using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Backend.Models;
using Backend.Services;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AppDbContext _context;
    private readonly IConfiguration _config;
    private readonly GratisMeldingWachtrij _gratisMeldingen;

    public AdminController(UserManager<ApplicationUser> userManager, AppDbContext context, IConfiguration config, GratisMeldingWachtrij gratisMeldingen)
    {
        _userManager = userManager;
        _context = context;
        _config = config;
        _gratisMeldingen = gratisMeldingen;
    }

    // Diagnose: welke betaalmethoden staat Mollie toe voor een abonnement (eerste betaling)?
    [HttpGet("mollie-diagnose")]
    public async Task<IActionResult> MollieDiagnose()
    {
        var sleutel = Environment.GetEnvironmentVariable("MOLLIE_API_KEY") ?? _config["Mollie:ApiKey"];
        if (string.IsNullOrEmpty(sleutel)) return Ok(new { fout = "Geen Mollie API-sleutel ingesteld op de server." });

        var client = new Mollie.Api.Client.PaymentMethodClient(sleutel);
        async Task<object> Lijst(string? sequenceType, string bedrag)
        {
            try
            {
                var r = await client.GetPaymentMethodListAsync(sequenceType: sequenceType,
                    amount: new Mollie.Api.Models.Amount(Mollie.Api.Models.Currency.EUR, bedrag));
                return r.Items.Select(m => m.Id).ToList();
            }
            catch (Exception ex) { return "Fout: " + ex.Message; }
        }
        async Task<string> Methode(string id)
        {
            try { var m = await client.GetPaymentMethodAsync(id); return "beschikbaar (" + m.Description + ")"; }
            catch (Exception ex) { return "niet beschikbaar: " + ex.Message; }
        }

        return Ok(new
        {
            modus = sleutel.StartsWith("live_") ? "live" : sleutel.StartsWith("test_") ? "test" : "onbekend",
            abonnementKoppelen_0_01 = await Lijst("first", "0.01"),
            abonnementStarten_5_00 = await Lijst("first", "5.00"),
            losseBetaling_10_00 = await Lijst("oneoff", "10.00"),
            ideal = await Methode("ideal"),
            sepaIncasso = await Methode("directdebit")
        });
    }

    [HttpGet("gebruikers")]
    public async Task<IActionResult> GetGebruikers()
    {
        var gekoppeld = await _context.MollieKoppelingen
            .Select(k => new { k.GebruikerId, k.KanBetalingenOntvangen, k.OnboardingStatus })
            .ToDictionaryAsync(k => k.GebruikerId);
        var metIban = (await _context.Coaches.Where(c => c.Iban != null && c.GebruikerId != null)
            .Select(c => c.GebruikerId!).ToListAsync()).ToHashSet();
        var gebruikers = await _userManager.Users
            .Select(u => new { u.Id, u.Naam, u.Email, u.Rol, u.MarketingToestemming, u.AangemaaktOp, u.AbonnementActief, u.AbonnementVerlooptOp, u.GratisVerlooptOp })
            .ToListAsync();
        return Ok(gebruikers.Select(u => new
        {
            u.Id, u.Naam, u.Email, u.Rol, u.MarketingToestemming, u.AangemaaktOp, u.AbonnementActief, u.AbonnementVerlooptOp, u.GratisVerlooptOp,
            // null = geen Mollie gekoppeld, "klaar" = kan betaald worden, anders de status bij Mollie
            Mollie = gekoppeld.TryGetValue(u.Id, out var k)
                ? (k.KanBetalingenOntvangen ? "klaar" : k.OnboardingStatus ?? "bezig")
                : null,
            // Rekeningnummer ingevuld voor betalen via overmaken
            Overmaken = metIban.Contains(u.Id)
        }));
    }

    // Servicekosten (2%) die via Mollie Connect zijn binnengekomen, per maand en per coach.
    // Mollie vraagt dat je coaches hiervoor een factuur stuurt.
    [HttpGet("servicekosten")]
    public async Task<IActionResult> Servicekosten()
    {
        var boekingen = await _context.Boekingen
            .Where(b => b.MollieModus == "koppeling" && b.Servicekosten != null && (b.Status == "Betaald" || b.Status == "Voldaan"))
            .Select(b => new { b.CoachGebruikerId, b.AangemaaktOp, b.Bedrag, b.Servicekosten })
            .ToListAsync();
        var coachIds = boekingen.Select(b => b.CoachGebruikerId).Distinct().ToList();
        var coaches = await _userManager.Users.Where(u => coachIds.Contains(u.Id))
            .Select(u => new { u.Id, u.Naam, u.Email }).ToDictionaryAsync(u => u.Id);
        var regels = boekingen
            .GroupBy(b => new { Maand = b.AangemaaktOp.ToString("yyyy-MM"), b.CoachGebruikerId })
            .Select(g => new
            {
                maand = g.Key.Maand,
                coach = coaches.TryGetValue(g.Key.CoachGebruikerId, out var c) ? c.Naam : "(verwijderd)",
                email = coaches.TryGetValue(g.Key.CoachGebruikerId, out var c2) ? c2.Email : null,
                aantal = g.Count(),
                omzetCoach = g.Sum(b => b.Bedrag),
                servicekosten = g.Sum(b => b.Servicekosten ?? 0)
            })
            .OrderByDescending(r => r.maand).ThenBy(r => r.coach)
            .ToList();
        return Ok(new
        {
            ingesteld = MollieConnectService.IsIngesteld,
            testmodus = MollieConnectService.Testmodus,
            regels
        });
    }

    [HttpDelete("gebruikers/{id}")]
    public async Task<IActionResult> VerwijderGebruiker(string id, [FromServices] AccountVerwijderService verwijderService)
    {
        var gebruiker = await _userManager.FindByIdAsync(id);
        if (gebruiker == null) return NotFound();
        if ((gebruiker.Rol ?? "").Split(',').Any(r => r.Trim() == "Admin")) return BadRequest("Het admin-account kan niet verwijderd worden.");

        // Alles van deze gebruiker weg (profiel, berichten, boekingen, reviews, ...), net als bij zelf verwijderen
        var fout = await verwijderService.VerwijderAsync(id);
        if (fout != null) return BadRequest(fout);
        return NoContent();
    }

    // Gratis periode van een coach aanpassen. Beveiligd tegen vergissingen:
    // alleen coaches, max 24 maanden per keer, en de naam van de coach moet
    // als bevestiging worden meegestuurd.
    [HttpPost("gebruikers/{id}/gratis")]
    public async Task<IActionResult> PasGratisAan(string id, [FromBody] GratisAanpassenVerzoek verzoek)
    {
        var gebruiker = await _userManager.FindByIdAsync(id);
        if (gebruiker == null) return NotFound("Gebruiker niet gevonden.");
        if (!gebruiker.Rol.Split(',').Any(r => r.Trim() == "Coach"))
            return BadRequest("Alleen coaches kunnen een gratis periode krijgen.");

        if (!string.Equals((verzoek.Bevestiging ?? "").Trim(), gebruiker.Naam.Trim(), StringComparison.OrdinalIgnoreCase))
            return BadRequest("Bevestiging klopt niet: typ exact de naam van de coach.");

        var nu = DateTime.UtcNow;
        var vorige = gebruiker.GratisVerlooptOp;
        var altijd = new DateTime(9999, 12, 31, 0, 0, 0, DateTimeKind.Utc);

        switch (verzoek.Actie)
        {
            case "maanden":
                if (verzoek.Maanden is null or < 1 or > 24)
                    return BadRequest("Kies tussen 1 en 24 maanden.");
                if (vorige.HasValue && vorige.Value.Year >= 9999)
                    return BadRequest("Deze coach is al voor altijd gratis.");
                // Verleng vanaf de huidige einddatum als die nog loopt, anders vanaf nu
                var basis = vorige.HasValue && vorige.Value > nu ? vorige.Value : nu;
                gebruiker.GratisVerlooptOp = basis.AddMonths(verzoek.Maanden.Value);
                break;
            case "altijd":
                if (vorige.HasValue && vorige.Value.Year >= 9999)
                    return BadRequest("Deze coach is al voor altijd gratis.");
                gebruiker.GratisVerlooptOp = altijd;
                break;
            case "herstel":
                // Alleen bedoeld voor 'Ongedaan maken' direct na een wijziging
                gebruiker.GratisVerlooptOp = verzoek.HerstelNaar;
                break;
            default:
                return BadRequest("Onbekende actie.");
        }

        var resultaat = await _userManager.UpdateAsync(gebruiker);
        if (!resultaat.Succeeded)
            return BadRequest(string.Join(", ", resultaat.Errors.Select(e => e.Description)));

        // Coach krijgt (na een korte wachttijd, zodat 'Ongedaan maken' nog kan) een mail en pushmelding
        if (verzoek.Actie != "herstel" && gebruiker.GratisVerlooptOp.HasValue)
            _gratisMeldingen.Voeg(new GratisMeldingTaak(gebruiker.Id, vorige, gebruiker.GratisVerlooptOp.Value));

        return Ok(new { vorige, nieuwe = gebruiker.GratisVerlooptOp, meldingNaMinuten = (int)GratisMeldingWachtrij.Wachttijd.TotalMinutes });
    }

    [HttpGet("coaches")]
    public async Task<IActionResult> GetAlleCoaches()
    {
        var coaches = await _context.Coaches.OrderBy(c => c.Naam).ToListAsync();
        return Ok(coaches);
    }

    [HttpDelete("coaches/{id}")]
    public async Task<IActionResult> VerwijderCoach(int id)
    {
        var coach = await _context.Coaches.FindAsync(id);
        if (coach == null) return NotFound();
        _context.Coaches.Remove(coach);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("rijders")]
    public async Task<IActionResult> GetAlleRijders()
    {
        var rijders = await _context.Rijders.OrderBy(r => r.Naam).ToListAsync();
        return Ok(rijders);
    }

    [HttpDelete("rijders/{id}")]
    public async Task<IActionResult> VerwijderRijder(int id)
    {
        var rijder = await _context.Rijders.FindAsync(id);
        if (rijder == null) return NotFound();
        _context.Rijders.Remove(rijder);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("coach-uitnodiging")]
    public async Task<IActionResult> MaakCoachUitnodiging([FromBody] CoachUitnodigingVerzoek verzoek)
    {
        if (string.IsNullOrWhiteSpace(verzoek.Naam))
            return BadRequest("Naam is verplicht.");

        var code = System.Security.Cryptography.RandomNumberGenerator.GetInt32(10000000, 100000000).ToString();
        var wachtwoord = GenereerWachtwoord();

        var gebruiker = new ApplicationUser
        {
            UserName = code,
            Email = code + "@uitnodiging.racecoachfinder.nl",
            NormalizedEmail = (code + "@uitnodiging.racecoachfinder.nl").ToUpperInvariant(),
            Naam = verzoek.Naam.Trim(),
            Rol = "Coach",
            EmailConfirmed = true,
            HeeftAccountIngericht = false,
            // Voor altijd gratis = einddatum 31-12-9999 (bestaande controles blijven zo werken)
            GratisVerlooptOp = verzoek.VoorAltijdGratis
                ? new DateTime(9999, 12, 31, 0, 0, 0, DateTimeKind.Utc)
                : DateTime.UtcNow.AddMonths(verzoek.GratisMananden),
            AangemaaktOp = DateTime.UtcNow
        };

        var resultaat = await _userManager.CreateAsync(gebruiker, wachtwoord);
        if (!resultaat.Succeeded)
            return BadRequest(string.Join(", ", resultaat.Errors.Select(e => e.Description)));

        return Ok(new { code, wachtwoord, naam = gebruiker.Naam, gratisMananden = verzoek.GratisMananden, voorAltijdGratis = verzoek.VoorAltijdGratis });
    }

    private static string GenereerWachtwoord()
    {
        const string tekens = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghjkmnpqrstuvwxyz23456789";
        return new string(Enumerable.Range(0, 10).Select(_ => tekens[System.Security.Cryptography.RandomNumberGenerator.GetInt32(tekens.Length)]).ToArray());
    }
}

public record GratisAanpassenVerzoek(string Actie, int? Maanden, DateTime? HerstelNaar, string? Bevestiging);
public record CoachUitnodigingVerzoek(string Naam, int GratisMananden = 3, bool VoorAltijdGratis = false);
