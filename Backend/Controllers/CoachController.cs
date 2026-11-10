using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Backend.Data;
using Backend.Models;
using Backend.Services;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CoachController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailService _email;
    private readonly ILogger<CoachController> _logger;
    private readonly IConfiguration _config;

    public CoachController(AppDbContext context, UserManager<ApplicationUser> userManager, IEmailService email, ILogger<CoachController> logger, IConfiguration config)
    {
        _context = context;
        _userManager = userManager;
        _email = email;
        _logger = logger;
        _config = config;
    }

    // Publiek: alleen gepubliceerde coaches (met gemiddelde score)
    [HttpGet]
    public async Task<IActionResult> GetCoaches(
        string? naam, string? locatie, string? discipline, string? specialisatie, bool? onlineCoaching,
        bool? geverifieerd, [FromServices] MollieConnectService connect)
    {
        var query = _context.Coaches.Where(c => c.IsGepubliceerd
            && !_context.GebruikerInstellingen.Any(i => i.GebruikerId == c.GebruikerId && !i.ProfielOpenbaar)).AsQueryable();

        if (!string.IsNullOrWhiteSpace(naam))
            query = query.Where(c => c.Naam.ToLower().Contains(naam.ToLower()));
        if (!string.IsNullOrWhiteSpace(locatie))
            query = query.Where(c => c.Woonplaats.ToLower().Contains(locatie.ToLower()));
        if (!string.IsNullOrWhiteSpace(discipline))
            query = query.Where(c => c.Discipline.Contains(discipline));
        if (!string.IsNullOrWhiteSpace(specialisatie))
            query = query.Where(c => c.Specialisatie.Contains(specialisatie));
        if (onlineCoaching.HasValue)
            query = query.Where(c => c.OnlineCoaching == onlineCoaching.Value);

        var coaches = await query.AsNoTracking().OrderBy(c => c.Naam).ToListAsync();
        // "Zakelijk geverifieerd": werkend Mollie-account (geen voorrang in de volgorde, wel een filter)
        var geverifieerdeIds = await connect.ZakelijkGeverifieerdAsync(coaches.Select(c => c.GebruikerId));
        coaches.ForEach(c => c.ZakelijkGeverifieerd = c.GebruikerId != null && geverifieerdeIds.Contains(c.GebruikerId));
        if (geverifieerd == true) coaches = coaches.Where(c => c.ZakelijkGeverifieerd).ToList();
        await VoegRatingsToe(coaches);
        coaches.ForEach(VerbergPriveGegevens);
        return Ok(coaches);
    }

    // Publiek: één coach bekijken + weergave tellen
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetCoach(int id, [FromServices] MollieConnectService connect)
    {
        var coach = await _context.Coaches.FindAsync(id);
        if (coach == null) return NotFound();
        if (!coach.IsGepubliceerd)
        {
            var gebruikerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (coach.GebruikerId != gebruikerId && !User.IsInRole("Admin"))
                return NotFound();
        }
        else
        {
            coach.AantalWeergaven++;
            await _context.SaveChangesAsync();
        }
        await VoegRatingsToe([coach]);
        coach.GemiddeldeReactietijdUren = await BerekenReactietijd(coach.GebruikerId);
        coach.ZakelijkGeverifieerd = (await connect.ZakelijkGeverifieerdAsync(new[] { coach.GebruikerId })).Count > 0;
        // Factuurgegevens en e-mail alleen voor de coach zelf (en de admin)
        if (coach.GebruikerId != User.FindFirstValue(ClaimTypes.NameIdentifier) && !User.IsInRole("Admin"))
        {
            _context.Entry(coach).State = EntityState.Detached;
            VerbergPriveGegevens(coach);
        }
        return Ok(coach);
    }

    // Niet openbaar: e-mail, adres, telefoon, KvK en btw van de coach
    private static void VerbergPriveGegevens(Coach c)
    {
        c.Email = string.Empty;
        c.FactuurAdres = null;
        c.FactuurPostcode = null;
        c.FactuurStad = null;
        c.FactuurLand = null;
        c.FactuurTelefoon = null;
        c.KvkNummer = null;
        c.BtwNummer = null;
        c.Iban = null;
        c.IbanTenaamstelling = null;
    }

    private async Task<double> BerekenReactietijd(string? coachId)
    {
        if (string.IsNullOrEmpty(coachId)) return -1;
        var berichten = await _context.Berichten
            .Where(b => b.VanGebruikerId == coachId || b.NaarGebruikerId == coachId)
            .OrderBy(b => b.AangemaaktOp)
            .ToListAsync();

        var partners = berichten
            .Select(b => b.VanGebruikerId == coachId ? b.NaarGebruikerId : b.VanGebruikerId)
            .Distinct().ToList();

        var tijden = new List<double>();
        foreach (var partnerId in partners)
        {
            var gesprek = berichten
                .Where(b => (b.VanGebruikerId == coachId && b.NaarGebruikerId == partnerId) ||
                            (b.VanGebruikerId == partnerId && b.NaarGebruikerId == coachId))
                .OrderBy(b => b.AangemaaktOp).ToList();

            for (int i = 0; i < gesprek.Count - 1; i++)
            {
                if (gesprek[i].VanGebruikerId != coachId && gesprek[i + 1].VanGebruikerId == coachId)
                    tijden.Add((gesprek[i + 1].AangemaaktOp - gesprek[i].AangemaaktOp).TotalHours);
            }
        }
        return tijden.Count > 0 ? tijden.Average() : -1;
    }

    private async Task VoegRatingsToe(List<Coach> coaches)
    {
        var userIds = coaches.Select(c => c.GebruikerId).Where(id => id != null).ToList();
        var ratings = await _context.Reviews
            .Where(r => userIds.Contains(r.CoachGebruikerId))
            .GroupBy(r => r.CoachGebruikerId)
            .Select(g => new { Id = g.Key, Gemiddelde = g.Average(r => (double)r.Sterren), Aantal = g.Count() })
            .ToListAsync();

        foreach (var coach in coaches)
        {
            var r = ratings.FirstOrDefault(x => x.Id == coach.GebruikerId);
            coach.GemiddeldeScore = r?.Gemiddelde ?? 0.0;
            coach.AantalReviews = r?.Aantal ?? 0;
        }
    }

    // Coach: eigen profiel ophalen
    [HttpGet("mijn")]
    [Authorize(Roles = "Coach")]
    public async Task<ActionResult<Coach>> GetMijnProfiel()
    {
        var gebruikerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var coach = await _context.Coaches.FirstOrDefaultAsync(c => c.GebruikerId == gebruikerId);
        if (coach == null) return NotFound();
        return coach;
    }

    // Coach: profiel aanmaken
    [HttpPost]
    [Authorize(Roles = "Coach")]
    public async Task<ActionResult<Coach>> PostCoach(Coach coach)
    {
        var gebruikerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (await _context.Coaches.AnyAsync(c => c.GebruikerId == gebruikerId))
            return BadRequest("Je hebt al een coachprofiel.");

        coach.Id = 0;
        coach.GebruikerId = gebruikerId;
        coach.IsGepubliceerd = false;
        coach.AantalWeergaven = 0;
        _context.Coaches.Add(coach);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetCoach), new { id = coach.Id }, coach);
    }

    // Coach: eigen profiel bijwerken
    [HttpPut("mijn")]
    [Authorize(Roles = "Coach")]
    public async Task<IActionResult> PutMijnProfiel(Coach coach)
    {
        var gebruikerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var bestaand = await _context.Coaches.FirstOrDefaultAsync(c => c.GebruikerId == gebruikerId);
        if (bestaand == null) return NotFound();

        bestaand.Naam = coach.Naam;
        bestaand.Email = coach.Email;
        bestaand.Woonplaats = coach.Woonplaats;
        bestaand.Nationaliteit = coach.Nationaliteit;
        bestaand.Talen = coach.Talen;
        bestaand.Bio = coach.Bio;
        bestaand.Leeftijd = coach.Leeftijd;
        bestaand.JarenErvaring = coach.JarenErvaring;
        bestaand.Discipline = coach.Discipline;
        bestaand.Specialisatie = coach.Specialisatie;
        bestaand.BehaaldResultaten = coach.BehaaldResultaten;
        bestaand.BeschikbareBanen = coach.BeschikbareBanen;
        bestaand.OnlineCoaching = coach.OnlineCoaching;
        bestaand.PrijsPakketten = coach.PrijsPakketten;
        if (!string.IsNullOrEmpty(coach.FotoUrl)) bestaand.FotoUrl = coach.FotoUrl;
        bestaand.Instagram = coach.Instagram;
        bestaand.Kwaliteiten = coach.Kwaliteiten;
        bestaand.FactuurAdres = coach.FactuurAdres;
        bestaand.FactuurPostcode = coach.FactuurPostcode;
        bestaand.FactuurStad = coach.FactuurStad;
        bestaand.FactuurLand = coach.FactuurLand;
        bestaand.FactuurTelefoon = coach.FactuurTelefoon;
        bestaand.KvkNummer = coach.KvkNummer;
        bestaand.BtwNummer = coach.BtwNummer;

        await _context.SaveChangesAsync();
        return Ok(bestaand);
    }

    // Coach: rekeningnummer voor betalen via overmaken (zolang er geen werkend Mollie-account is)
    public record OvermakenVerzoek(string? Iban, string? Tenaamstelling);

    [HttpPut("mijn/overmaken")]
    [Authorize(Roles = "Coach")]
    public async Task<IActionResult> PutOvermaken([FromBody] OvermakenVerzoek verzoek)
    {
        var gebruikerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var coach = await _context.Coaches.FirstOrDefaultAsync(c => c.GebruikerId == gebruikerId);
        if (coach == null) return NotFound("Maak eerst je coachprofiel aan.");

        var leeg = string.IsNullOrWhiteSpace(verzoek.Iban) && string.IsNullOrWhiteSpace(verzoek.Tenaamstelling);
        if (leeg)
        {
            coach.Iban = null;
            coach.IbanTenaamstelling = null;
        }
        else
        {
            var iban = IbanHelper.Normaliseer(verzoek.Iban);
            if (iban == null) return BadRequest("Dit rekeningnummer (IBAN) klopt niet. Controleer het even.");
            var naam = (verzoek.Tenaamstelling ?? "").Trim();
            if (naam.Length < 2) return BadRequest("Vul de naam in waarop de rekening staat.");
            if (naam.Length > 100) return BadRequest("De tenaamstelling is te lang.");
            coach.Iban = iban;
            coach.IbanTenaamstelling = naam;
        }
        await _context.SaveChangesAsync();
        return Ok(new { iban = IbanHelper.Opmaak(coach.Iban), tenaamstelling = coach.IbanTenaamstelling });
    }

    // Coach: publiceren / depubliceren
    [HttpPost("mijn/publiceer")]
    [Authorize(Roles = "Coach")]
    public async Task<IActionResult> TogglePubliceer()
    {
        var gebruikerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var coach = await _context.Coaches.FirstOrDefaultAsync(c => c.GebruikerId == gebruikerId);
        if (coach == null) return NotFound();

        // Alleen publiceren toestaan als abonnement of gratis periode actief is
        if (!coach.IsGepubliceerd)
        {
            var gebruiker = await _userManager.FindByIdAsync(gebruikerId!);
            var nu = DateTime.UtcNow;
            var heeftToegang = gebruiker != null && (
                (gebruiker.AbonnementActief && gebruiker.AbonnementVerlooptOp > nu) ||
                (gebruiker.GratisVerlooptOp.HasValue && gebruiker.GratisVerlooptOp > nu)
            );
            if (!heeftToegang)
                return BadRequest("Geen actief abonnement. Abonneer eerst voor €10/maand.");
        }

        coach.IsGepubliceerd = !coach.IsGepubliceerd;
        await _context.SaveChangesAsync();
        return Ok(new { isGepubliceerd = coach.IsGepubliceerd });
    }

    [HttpPost("{id}/rapporteren")]
    [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("formulier")]
    public async Task<IActionResult> RapporteerCoach(int id, [FromBody] CoachRapportVerzoek verzoek)
    {
        if (!string.IsNullOrWhiteSpace(verzoek.Website)) return Ok(); // lokveld voor bots
        if ((verzoek.Reden ?? "").Length > 2000) return BadRequest("Je melding is te lang (maximaal 2000 tekens).");
        if (string.IsNullOrWhiteSpace(verzoek.Reden))
            return BadRequest("Geef een reden op.");

        var coach = await _context.Coaches.FindAsync(id);
        if (coach == null) return NotFound();

        var melderInfo = "Anoniem";
        var gebruikerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (gebruikerId != null)
        {
            var melder = await _userManager.FindByIdAsync(gebruikerId);
            if (melder != null)
                melderInfo = $"{melder.Naam} ({melder.Email})";
        }

        var adminEmail = _config["Email:Afzender"] ?? "racecoachfinder@gmail.com";
        var coachUrl = $"https://www.racecoachfinder.nl/coach-detail.html?id={id}";

        _ = _email.VerstuurAsync(adminEmail, "RaceCoachFinder Admin",
            $"Melding over coach: {coach.Naam}",
            EmailTemplates.CoachRapport(coach.Naam, id, verzoek.Reden, melderInfo, coachUrl))
            .ContinueWith(t => _logger.LogError(t.Exception, "Rapportage-mail mislukt voor coach {Id}", id),
                TaskContinuationOptions.OnlyOnFaulted);

        return Ok();
    }
}

public record CoachRapportVerzoek(string Reden, string? Website = null);
