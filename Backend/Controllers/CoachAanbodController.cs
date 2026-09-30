using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Backend.Models;
using System.Security.Claims;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CoachAanbodController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public CoachAanbodController(AppDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAlle(
        [FromQuery] string? categorie,
        [FromQuery] string? discipline,
        [FromQuery] string? locatie,
        [FromQuery] DateTime? datumVan,
        [FromQuery] bool? gratis)
    {
        var nu = DateTime.UtcNow.Date;
        var query = _context.CoachAanboden
            .Where(a => a.IsActief && a.Datum.Date >= nu);

        if (!string.IsNullOrWhiteSpace(categorie))
            query = query.Where(a => a.Categorieen.Contains(categorie));

        if (!string.IsNullOrWhiteSpace(discipline))
            query = query.Where(a => a.Disciplines.Contains(discipline));

        if (!string.IsNullOrWhiteSpace(locatie))
            query = query.Where(a => a.Locatie.ToLower().Contains(locatie.ToLower()));

        if (datumVan.HasValue)
            query = query.Where(a => a.Datum.Date >= datumVan.Value.Date);

        if (gratis.HasValue)
            query = query.Where(a => a.IsGratis == gratis.Value);

        var lijst = await query.OrderBy(a => a.Datum).ToListAsync();

        // Voeg CoachProfielId + review-stats toe via join op GebruikerId
        var gebruikerIds = lijst.Select(a => a.CoachGebruikerId).Distinct().ToList();

        var profielIds = await _context.Coaches
            .Where(c => c.GebruikerId != null && gebruikerIds.Contains(c.GebruikerId))
            .ToDictionaryAsync(c => c.GebruikerId!, c => c.Id);

        var reviewStats = await _context.Reviews
            .Where(r => gebruikerIds.Contains(r.CoachGebruikerId))
            .GroupBy(r => r.CoachGebruikerId)
            .Select(g => new { CoachGebruikerId = g.Key, Gemiddeld = g.Average(r => (double)r.Sterren), Aantal = g.Count() })
            .ToDictionaryAsync(x => x.CoachGebruikerId);

        // Groepsgesprekken + geaccepteerde leden per aanbod
        var aanbodIds = lijst.Select(a => a.Id).ToList();
        var groepen = await _context.Groepsgesprekken
            .Where(g => aanbodIds.Contains(g.CoachAanbodId))
            .ToDictionaryAsync(g => g.CoachAanbodId);

        var groepIds = groepen.Values.Select(g => g.Id).ToList();
        var alleLeden = await _context.GroepsgesprekLeden
            .Where(l => groepIds.Contains(l.GroepsgesprekId))
            .ToListAsync();

        var resultaat = lijst.Select(a => {
            groepen.TryGetValue(a.Id, out var groep);
            var leden = groep != null
                ? alleLeden.Where(l => l.GroepsgesprekId == groep.Id && l.GebruikerId != a.CoachGebruikerId)
                           .Select(l => l.GebruikerNaam).ToList()
                : new List<string>();
            return new {
                a.Id,
                a.CoachGebruikerId,
                CoachProfielId = profielIds.TryGetValue(a.CoachGebruikerId, out var pid) ? pid : (int?)null,
                a.CoachNaam,
                a.Titel,
                a.KorteOmschrijving,
                a.Beschrijving,
                a.Datum,
                a.Locatie,
                a.Categorieen,
                a.Disciplines,
                a.MaxRijders,
                a.PrijsPerRijder,
                a.IsGratis,
                a.AangemaaktOp,
                a.IsActief,
                GemiddeldeScore = reviewStats.TryGetValue(a.CoachGebruikerId, out var rs) ? rs.Gemiddeld : 0,
                AantalReviews = reviewStats.TryGetValue(a.CoachGebruikerId, out var rs2) ? rs2.Aantal : 0,
                GroepsgesprekId = groep?.Id,
                GeaccepteerdeDeelnemers = leden
            };
        });

        return Ok(resultaat);
    }

    [HttpGet("mijn")]
    [Authorize(Roles = "Coach")]
    public async Task<IActionResult> GetMijn()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var lijst = await _context.CoachAanboden
            .Where(a => a.CoachGebruikerId == userId)
            .OrderByDescending(a => a.AangemaaktOp)
            .ToListAsync();
        return Ok(lijst);
    }

    [HttpPost]
    [Authorize(Roles = "Coach")]
    public async Task<IActionResult> Aanmaken([FromBody] CoachAanbodVerzoek verzoek)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var gebruiker = await _userManager.FindByIdAsync(userId!);
        if (gebruiker == null) return Unauthorized();

        if (string.IsNullOrWhiteSpace(verzoek.Titel))
            return BadRequest("Titel is verplicht.");
        if (string.IsNullOrWhiteSpace(verzoek.Locatie))
            return BadRequest("Locatie is verplicht.");
        if (verzoek.Datum == default)
            return BadRequest("Datum is verplicht.");
        if (verzoek.Datum.Date < DateTime.UtcNow.Date)
            return BadRequest("Datum moet in de toekomst liggen.");

        var aanbod = new CoachAanbod
        {
            CoachGebruikerId = userId!,
            CoachNaam = gebruiker.Naam,
            Titel = verzoek.Titel.Trim(),
            KorteOmschrijving = verzoek.KorteOmschrijving?.Trim() ?? string.Empty,
            Beschrijving = verzoek.Beschrijving?.Trim() ?? string.Empty,
            Datum = verzoek.Datum,
            Locatie = verzoek.Locatie.Trim(),
            Categorieen = verzoek.Categorieen ?? string.Empty,
            Disciplines = verzoek.Disciplines ?? string.Empty,
            MaxRijders = verzoek.MaxRijders,
            PrijsPerRijder = verzoek.PrijsPerRijder,
            IsGratis = verzoek.IsGratis,
            AangemaaktOp = DateTime.UtcNow,
            IsActief = true
        };

        _context.CoachAanboden.Add(aanbod);
        await _context.SaveChangesAsync();

        // Maak groepsgesprek aan voor deze aanbieding
        var groep = new Groepsgesprek
        {
            CoachAanbodId = aanbod.Id,
            Naam = aanbod.Titel,
            AangemaaktDoorId = userId!,
            AangemaaktOp = DateTime.UtcNow
        };
        _context.Groepsgesprekken.Add(groep);
        await _context.SaveChangesAsync();

        // Voeg maker toe als lid
        _context.GroepsgesprekLeden.Add(new GroepsgesprekLid
        {
            GroepsgesprekId = groep.Id,
            GebruikerId = userId!,
            GebruikerNaam = gebruiker.Naam,
            ToegetreedOp = DateTime.UtcNow
        });

        // Verwerk uitgenodigde coaches
        if (verzoek.UitgenodigdeCoachIds != null)
        {
            foreach (var coachId in verzoek.UitgenodigdeCoachIds.Distinct())
            {
                var coach = await _userManager.FindByIdAsync(coachId);
                if (coach == null) continue;

                var uitnodiging = new CoachUitnodiging
                {
                    CoachAanbodId = aanbod.Id,
                    AanbodTitel = aanbod.Titel,
                    RijderGebruikerId = userId!,
                    RijderNaam = gebruiker.Naam,
                    CoachGebruikerId = coachId,
                    CoachNaam = coach.Naam,
                    Status = "Openstaand",
                    AangemaaktOp = DateTime.UtcNow
                };
                _context.CoachAanbodUitnodigingen.Add(uitnodiging);
                await _context.SaveChangesAsync();

                var datumStr = aanbod.Datum.ToString("d MMM yyyy");
                var prijsStr = aanbod.IsGratis ? "Gratis" : (aanbod.PrijsPerRijder.HasValue ? $"€{aanbod.PrijsPerRijder} p.p." : "");
                _context.Berichten.Add(new Bericht
                {
                    VanGebruikerId = userId!,
                    NaarGebruikerId = coachId,
                    Tekst = $"🎯 UITNODIGING:{uitnodiging.Id}\n{gebruiker.Naam} nodigt je uit voor: {aanbod.Titel}\n📍 {aanbod.Locatie} | 📅 {datumStr}{(prijsStr != "" ? " | " + prijsStr : "")}",
                    AangemaaktOp = DateTime.UtcNow,
                    Gelezen = false
                });
            }
        }

        await _context.SaveChangesAsync();
        return Ok(new { aanbod.Id, groepsgesprekId = groep.Id });
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Coach")]
    public async Task<IActionResult> Verwijderen(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var aanbod = await _context.CoachAanboden.FindAsync(id);
        if (aanbod == null) return NotFound();
        if (aanbod.CoachGebruikerId != userId) return Forbid();

        _context.CoachAanboden.Remove(aanbod);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}

public record CoachAanbodVerzoek(
    string Titel,
    string? KorteOmschrijving,
    string? Beschrijving,
    DateTime Datum,
    string Locatie,
    string? Categorieen,
    string? Disciplines,
    int? MaxRijders,
    decimal? PrijsPerRijder,
    bool IsGratis,
    List<string>? UitgenodigdeCoachIds = null
);
