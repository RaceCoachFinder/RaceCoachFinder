using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Backend.Data;
using Backend.Models;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RijderFavorietenController : ControllerBase
{
    private readonly AppDbContext _context;

    public RijderFavorietenController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetFavorieten()
    {
        var coachId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var ids = await _context.RijderFavorieten
            .Where(f => f.CoachId == coachId)
            .Select(f => f.RijderId)
            .ToListAsync();
        return Ok(ids);
    }

    [HttpPost("{rijderId}")]
    public async Task<IActionResult> Toevoegen(string rijderId)
    {
        var coachId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var bestaand = await _context.RijderFavorieten
            .FirstOrDefaultAsync(f => f.CoachId == coachId && f.RijderId == rijderId);
        if (bestaand != null) return Ok();
        _context.RijderFavorieten.Add(new RijderFavoriet { CoachId = coachId, RijderId = rijderId });
        await _context.SaveChangesAsync();
        return Ok();
    }

    [HttpDelete("{rijderId}")]
    public async Task<IActionResult> Verwijderen(string rijderId)
    {
        var coachId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var favoriet = await _context.RijderFavorieten
            .FirstOrDefaultAsync(f => f.CoachId == coachId && f.RijderId == rijderId);
        if (favoriet == null) return Ok();
        _context.RijderFavorieten.Remove(favoriet);
        await _context.SaveChangesAsync();
        return Ok();
    }
}
