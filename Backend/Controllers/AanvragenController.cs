using Microsoft.AspNetCore.Authorization;
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

    public AanvragenController(AppDbContext context)
    {
        _context = context;
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
}
