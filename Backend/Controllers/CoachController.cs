using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Backend.Data;
using Backend.Models;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CoachController : ControllerBase
{
    private readonly AppDbContext _context;

    public CoachController(AppDbContext context)
    {
        _context = context;
    }

    // Publiek: alleen gepubliceerde coaches (met gemiddelde score)
    [HttpGet]
    public async Task<IActionResult> GetCoaches(
        string? naam, string? locatie, string? discipline, string? specialisatie, bool? onlineCoaching)
    {
        var query = _context.Coaches.Where(c => c.IsGepubliceerd).AsQueryable();

        if (!string.IsNullOrWhiteSpace(naam))
            query = query.Where(c => c.Naam.Contains(naam));
        if (!string.IsNullOrWhiteSpace(locatie))
            query = query.Where(c => c.Woonplaats.Contains(locatie));
        if (!string.IsNullOrWhiteSpace(discipline))
            query = query.Where(c => c.Discipline.Contains(discipline));
        if (!string.IsNullOrWhiteSpace(specialisatie))
            query = query.Where(c => c.Specialisatie.Contains(specialisatie));
        if (onlineCoaching.HasValue)
            query = query.Where(c => c.OnlineCoaching == onlineCoaching.Value);

        var coaches = await query.OrderBy(c => c.Naam).ToListAsync();
        await VoegRatingsToe(coaches);
        return Ok(coaches);
    }

    // Publiek: één coach bekijken + weergave tellen
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetCoach(int id)
    {
        var coach = await _context.Coaches.FindAsync(id);
        if (coach == null) return NotFound();
        if (!coach.IsGepubliceerd)
        {
            var gebruikerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var rol = User.FindFirstValue(ClaimTypes.Role);
            if (coach.GebruikerId != gebruikerId && rol != "Admin")
                return NotFound();
        }
        else
        {
            coach.AantalWeergaven++;
            await _context.SaveChangesAsync();
        }
        await VoegRatingsToe([coach]);
        coach.GemiddeldeReactietijdUren = await BerekenReactietijd(coach.GebruikerId);
        return Ok(coach);
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

        await _context.SaveChangesAsync();
        return Ok(bestaand);
    }

    // Coach: publiceren / depubliceren
    [HttpPost("mijn/publiceer")]
    [Authorize(Roles = "Coach")]
    public async Task<IActionResult> TogglePubliceer()
    {
        var gebruikerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var coach = await _context.Coaches.FirstOrDefaultAsync(c => c.GebruikerId == gebruikerId);
        if (coach == null) return NotFound();

        coach.IsGepubliceerd = !coach.IsGepubliceerd;
        await _context.SaveChangesAsync();
        return Ok(new { isGepubliceerd = coach.IsGepubliceerd });
    }
}
