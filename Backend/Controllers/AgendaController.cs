using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Backend.Data;
using Backend.Models;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Coach")]
public class AgendaController : ControllerBase
{
    private readonly AppDbContext _context;

    public AgendaController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAgenda()
    {
        var coachId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var items = await _context.AgendaItems
            .Where(a => a.CoachGebruikerId == coachId)
            .OrderBy(a => a.Datum)
            .ToListAsync();
        return Ok(items);
    }

    [HttpPost]
    public async Task<IActionResult> VoegToe([FromBody] AgendaVerzoek verzoek)
    {
        var coachId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var item = new AgendaItem
        {
            CoachGebruikerId = coachId!,
            Titel = verzoek.Titel.Trim(),
            Datum = verzoek.Datum,
            Notitie = verzoek.Notitie?.Trim(),
            GekoppeldeBoekingId = verzoek.GekoppeldeBoekingId
        };
        _context.AgendaItems.Add(item);
        await _context.SaveChangesAsync();
        return Ok(item);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Verwijder(int id)
    {
        var coachId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var item = await _context.AgendaItems.FindAsync(id);
        if (item == null || item.CoachGebruikerId != coachId) return NotFound();
        _context.AgendaItems.Remove(item);
        await _context.SaveChangesAsync();
        return Ok();
    }
}

public record AgendaVerzoek(string Titel, DateTime Datum, string? Notitie, int? GekoppeldeBoekingId);
