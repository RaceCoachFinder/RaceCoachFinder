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
public class FavorietenController : ControllerBase
{
    private readonly AppDbContext _context;

    public FavorietenController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetFavorieten()
    {
        var gebruikerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var ids = await _context.CoachFavorieten
            .Where(f => f.RijderId == gebruikerId)
            .Select(f => f.CoachId)
            .ToListAsync();
        return Ok(ids);
    }

    [HttpPost("{coachId:int}")]
    public async Task<IActionResult> Toevoegen(int coachId)
    {
        var gebruikerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var bestaand = await _context.CoachFavorieten
            .FirstOrDefaultAsync(f => f.RijderId == gebruikerId && f.CoachId == coachId);
        if (bestaand != null) return Ok();

        _context.CoachFavorieten.Add(new CoachFavoriet { RijderId = gebruikerId, CoachId = coachId });
        await _context.SaveChangesAsync();
        return Ok();
    }

    [HttpDelete("{coachId:int}")]
    public async Task<IActionResult> Verwijderen(int coachId)
    {
        var gebruikerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var favoriet = await _context.CoachFavorieten
            .FirstOrDefaultAsync(f => f.RijderId == gebruikerId && f.CoachId == coachId);
        if (favoriet == null) return Ok();

        _context.CoachFavorieten.Remove(favoriet);
        await _context.SaveChangesAsync();
        return Ok();
    }
}
