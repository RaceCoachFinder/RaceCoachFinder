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
public class GroepsgesprekController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public GroepsgesprekController(AppDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> GetMijnGroepen()
    {
        var mijnId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var groepIds = await _context.GroepsgesprekLeden
            .Where(l => l.GebruikerId == mijnId)
            .Select(l => l.GroepsgesprekId)
            .ToListAsync();

        var groepen = await _context.Groepsgesprekken
            .Where(g => groepIds.Contains(g.Id))
            .ToListAsync();

        var resultaat = new List<object>();

        foreach (var g in groepen)
        {
            var leden = await _context.GroepsgesprekLeden
                .Where(l => l.GroepsgesprekId == g.Id)
                .ToListAsync();

            var jouwLid = leden.FirstOrDefault(l => l.GebruikerId == mijnId);

            var aantalOngelezen = await _context.Groepsberichten
                .CountAsync(b => b.GroepsgesprekId == g.Id
                    && b.VanGebruikerId != mijnId
                    && b.AangemaaktOp > (jouwLid!.LaatstGelezen ?? DateTime.MinValue));

            var laatste = await _context.Groepsberichten
                .Where(b => b.GroepsgesprekId == g.Id)
                .OrderByDescending(b => b.AangemaaktOp)
                .FirstOrDefaultAsync();

            resultaat.Add(new
            {
                GroepsId = g.Id,
                g.Naam,
                g.CoachAanbodId,
                Leden = leden.Select(l => l.GebruikerNaam).ToList(),
                AantalOngelezen = aantalOngelezen,
                LaatsteBericht = laatste?.Tekst ?? "",
                LaatsteTijd = laatste?.AangemaaktOp ?? g.AangemaaktOp
            });
        }

        return Ok(resultaat);
    }

    [HttpGet("{id}/berichten")]
    public async Task<IActionResult> GetBerichten(int id)
    {
        var mijnId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var lid = await _context.GroepsgesprekLeden
            .FirstOrDefaultAsync(l => l.GroepsgesprekId == id && l.GebruikerId == mijnId);
        if (lid == null) return Forbid();

        var berichten = await _context.Groepsberichten
            .Where(b => b.GroepsgesprekId == id)
            .OrderBy(b => b.AangemaaktOp)
            .ToListAsync();

        lid.LaatstGelezen = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(berichten);
    }

    [HttpPost("{id}/bericht")]
    public async Task<IActionResult> StuurBericht(int id, [FromBody] GroepsBerichtVerzoek verzoek)
    {
        var mijnId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var lid = await _context.GroepsgesprekLeden
            .FirstOrDefaultAsync(l => l.GroepsgesprekId == id && l.GebruikerId == mijnId);
        if (lid == null) return Forbid();

        if (string.IsNullOrWhiteSpace(verzoek.Tekst))
            return BadRequest("Bericht mag niet leeg zijn.");

        var bericht = new Groepsbericht
        {
            GroepsgesprekId = id,
            VanGebruikerId = mijnId!,
            VanNaam = lid.GebruikerNaam,
            Tekst = verzoek.Tekst.Trim(),
            AangemaaktOp = DateTime.UtcNow
        };

        _context.Groepsberichten.Add(bericht);
        lid.LaatstGelezen = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(bericht);
    }

    [HttpDelete("{id}/verlaten")]
    public async Task<IActionResult> Verlaten(int id)
    {
        var mijnId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var lid = await _context.GroepsgesprekLeden
            .FirstOrDefaultAsync(l => l.GroepsgesprekId == id && l.GebruikerId == mijnId);
        if (lid == null) return NotFound();
        _context.GroepsgesprekLeden.Remove(lid);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("deelnemen/{aanbodId}")]
    public async Task<IActionResult> Deelnemen(int aanbodId)
    {
        var mijnId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var gebruiker = await _userManager.FindByIdAsync(mijnId!);
        if (gebruiker == null) return Unauthorized();

        var groep = await _context.Groepsgesprekken
            .FirstOrDefaultAsync(g => g.CoachAanbodId == aanbodId);

        if (groep == null)
            return NotFound("Groepsgesprek niet gevonden voor deze aanbieding.");

        var isAlLid = await _context.GroepsgesprekLeden
            .AnyAsync(l => l.GroepsgesprekId == groep.Id && l.GebruikerId == mijnId);

        if (!isAlLid)
        {
            _context.GroepsgesprekLeden.Add(new GroepsgesprekLid
            {
                GroepsgesprekId = groep.Id,
                GebruikerId = mijnId!,
                GebruikerNaam = gebruiker.Naam,
                ToegetreedOp = DateTime.UtcNow
            });
            _context.Groepsberichten.Add(new Groepsbericht
            {
                GroepsgesprekId = groep.Id,
                VanGebruikerId = mijnId!,
                VanNaam = gebruiker.Naam,
                Tekst = $"👋 {gebruiker.Naam} heeft gereageerd op de activiteit.",
                AangemaaktOp = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
        }

        return Ok(new { groepsgesprekId = groep.Id, groepsNaam = groep.Naam });
    }
}

public record GroepsBerichtVerzoek(string Tekst);
