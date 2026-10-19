using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Backend.Data;
using Backend.Models;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AanvragenController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public AanvragenController(AppDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // Publiek: actieve, niet-verlopen aanvragen
    [HttpGet]
    public async Task<IActionResult> GetAlle([FromQuery] bool? groep = null)
    {
        var vandaag = DateTime.Today;
        var query = _context.RijderAanvragen
            .Where(a => !a.IsGesloten && (!a.Datum.HasValue || a.Datum.Value.Date >= vandaag));

        if (groep.HasValue)
            query = query.Where(a => a.IsGroep == groep.Value);

        var aanvragen = await query
            .OrderByDescending(a => a.AangemaaktOp)
            .ToListAsync();
        return Ok(aanvragen);
    }

    // Eigen aanvragen (inclusief gesloten)
    [HttpGet("mijn")]
    [Authorize]
    public async Task<IActionResult> GetMijn()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var aanvragen = await _context.RijderAanvragen
            .Where(a => a.RijderId == userId)
            .OrderByDescending(a => a.AangemaaktOp)
            .ToListAsync();
        return Ok(aanvragen);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Aanmaken([FromBody] RijderAanvraag aanvraag)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var user = await _context.Users.FindAsync(userId) as ApplicationUser;
        aanvraag.RijderId = userId;
        aanvraag.RijderNaam = user?.Naam ?? user?.Email ?? "Onbekend";
        aanvraag.AangemaaktOp = DateTime.UtcNow;
        aanvraag.IsGesloten = false;
        aanvraag.CoachGevonden = false;
        _context.RijderAanvragen.Add(aanvraag);
        await _context.SaveChangesAsync();
        return Ok(aanvraag);
    }

    [HttpPut("{id:int}/sluit")]
    [Authorize]
    public async Task<IActionResult> Sluit(int id, [FromQuery] bool coachGevonden = false)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var aanvraag = await _context.RijderAanvragen.FindAsync(id);
        if (aanvraag == null) return NotFound();
        if (aanvraag.RijderId != userId) return Forbid();
        aanvraag.IsGesloten = true;
        aanvraag.CoachGevonden = coachGevonden;
        await _context.SaveChangesAsync();
        return Ok(new { success = true });
    }

    [HttpDelete("{id:int}")]
    [Authorize]
    public async Task<IActionResult> Verwijderen(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var aanvraag = await _context.RijderAanvragen.FindAsync(id);
        if (aanvraag == null) return NotFound();
        if (aanvraag.RijderId != userId) return Forbid();
        _context.RijderAanvragen.Remove(aanvraag);
        await _context.SaveChangesAsync();
        return Ok(new { success = true });
    }

    // Coach reageert op een groepstraining-aanvraag: vindt of maakt een groepsgesprek
    [HttpPost("{id:int}/groepschat")]
    [Authorize(Roles = "Coach")]
    public async Task<IActionResult> MaakGroepschat(int id)
    {
        var coachId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var aanvraag = await _context.RijderAanvragen.FindAsync(id);
        if (aanvraag == null) return NotFound();

        var coach = await _userManager.FindByIdAsync(coachId);
        var rijder = await _userManager.FindByIdAsync(aanvraag.RijderId);

        // Bestaat er al een chat voor deze aanvraag?
        var bestaand = await _context.Groepsgesprekken
            .FirstOrDefaultAsync(g => g.RijderAanvraagId == id);

        if (bestaand != null)
        {
            // Voeg de coach toe als die er nog niet in zit
            if (!await _context.GroepsgesprekLeden.AnyAsync(l => l.GroepsgesprekId == bestaand.Id && l.GebruikerId == coachId))
            {
                _context.GroepsgesprekLeden.Add(new GroepsgesprekLid
                {
                    GroepsgesprekId = bestaand.Id,
                    GebruikerId = coachId,
                    GebruikerNaam = coach?.Naam ?? "Coach",
                    ToegetreedOp = DateTime.UtcNow
                });
                _context.Groepsberichten.Add(new Groepsbericht
                {
                    GroepsgesprekId = bestaand.Id,
                    VanGebruikerId = coachId,
                    VanNaam = coach?.Naam ?? "Coach",
                    Tekst = $"➕ {coach?.Naam ?? "Coach"} heeft gereageerd op de aanvraag.",
                    AangemaaktOp = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();
            }
            return Ok(new { groepsgesprekId = bestaand.Id });
        }

        // Nieuw groepsgesprek aanmaken
        var korteBeschrijving = aanvraag.Beschrijving?.Length > 40
            ? aanvraag.Beschrijving[..40] + "…"
            : aanvraag.Beschrijving ?? "Groepstraining";

        var groep = new Groepsgesprek
        {
            CoachAanbodId = 0,
            RijderAanvraagId = id,
            Naam = $"Groepstraining: {korteBeschrijving}",
            AangemaaktDoorId = coachId,
            AangemaaktOp = DateTime.UtcNow
        };
        _context.Groepsgesprekken.Add(groep);
        await _context.SaveChangesAsync();

        var nu = DateTime.UtcNow;
        _context.GroepsgesprekLeden.Add(new GroepsgesprekLid
        {
            GroepsgesprekId = groep.Id, GebruikerId = coachId,
            GebruikerNaam = coach?.Naam ?? "Coach", ToegetreedOp = nu, LaatstGelezen = nu
        });
        if (rijder != null)
        {
            _context.GroepsgesprekLeden.Add(new GroepsgesprekLid
            {
                GroepsgesprekId = groep.Id, GebruikerId = rijder.Id,
                GebruikerNaam = rijder.Naam, ToegetreedOp = nu
            });
        }
        _context.Groepsberichten.Add(new Groepsbericht
        {
            GroepsgesprekId = groep.Id,
            VanGebruikerId = coachId,
            VanNaam = coach?.Naam ?? "Coach",
            Tekst = $"👥 {coach?.Naam ?? "Coach"} heeft een groepschat aangemaakt voor deze aanvraag.",
            AangemaaktOp = nu
        });
        await _context.SaveChangesAsync();

        return Ok(new { groepsgesprekId = groep.Id });
    }
}
