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
[Authorize]
public class CoachUitnodigingController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public CoachUitnodigingController(AppDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [HttpGet("mijn")]
    public async Task<IActionResult> GetMijn()
    {
        var mijnId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var lijst = await _context.CoachAanbodUitnodigingen
            .Where(u => u.CoachGebruikerId == mijnId && u.Status == "Openstaand")
            .OrderByDescending(u => u.AangemaaktOp)
            .ToListAsync();
        return Ok(lijst);
    }

    [HttpPut("{id}/accepteer")]
    public async Task<IActionResult> Accepteer(int id)
    {
        var mijnId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var uitnodiging = await _context.CoachAanbodUitnodigingen.FindAsync(id);
        if (uitnodiging == null || uitnodiging.CoachGebruikerId != mijnId)
            return NotFound();
        if (uitnodiging.Status != "Openstaand")
            return BadRequest("Uitnodiging is al verwerkt.");

        uitnodiging.Status = "Geaccepteerd";

        var groep = await _context.Groepsgesprekken
            .FirstOrDefaultAsync(g => g.CoachAanbodId == uitnodiging.CoachAanbodId);

        if (groep != null)
        {
            var isAlLid = await _context.GroepsgesprekLeden
                .AnyAsync(l => l.GroepsgesprekId == groep.Id && l.GebruikerId == mijnId);

            if (!isAlLid)
            {
                _context.GroepsgesprekLeden.Add(new GroepsgesprekLid
                {
                    GroepsgesprekId = groep.Id,
                    GebruikerId = mijnId!,
                    GebruikerNaam = uitnodiging.CoachNaam,
                    ToegetreedOp = DateTime.UtcNow
                });
                _context.Groepsberichten.Add(new Groepsbericht
                {
                    GroepsgesprekId = groep.Id,
                    VanGebruikerId = mijnId!,
                    VanNaam = uitnodiging.CoachNaam,
                    Tekst = $"✅ {uitnodiging.CoachNaam} heeft de uitnodiging geaccepteerd.",
                    AangemaaktOp = DateTime.UtcNow
                });
            }
        }

        await _context.SaveChangesAsync();
        return Ok(new { groepsgesprekId = groep?.Id, groepsNaam = groep?.Naam });
    }

    [HttpPut("{id}/wijs-af")]
    public async Task<IActionResult> WijsAf(int id)
    {
        var mijnId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var uitnodiging = await _context.CoachAanbodUitnodigingen.FindAsync(id);
        if (uitnodiging == null || uitnodiging.CoachGebruikerId != mijnId)
            return NotFound();
        if (uitnodiging.Status != "Openstaand")
            return BadRequest("Uitnodiging is al verwerkt.");

        uitnodiging.Status = "Afgewezen";
        await _context.SaveChangesAsync();
        return Ok();
    }
}
