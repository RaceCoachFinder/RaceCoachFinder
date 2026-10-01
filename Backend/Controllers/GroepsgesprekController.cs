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

    // Maak zelf een groepsgesprek aan met gekozen gebruikers
    [HttpPost]
    public async Task<IActionResult> MaakGroep([FromBody] NieuweGroepVerzoek verzoek)
    {
        var mijnId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var ik = await _userManager.FindByIdAsync(mijnId!);
        if (ik == null) return Unauthorized();

        var naam = (verzoek.Naam ?? "").Trim();
        if (naam.Length == 0) return BadRequest("Geef de groep een naam.");
        if (naam.Length > 80) return BadRequest("Groepsnaam mag maximaal 80 tekens zijn.");

        var ledenIds = (verzoek.LedenIds ?? new List<string>())
            .Where(id => !string.IsNullOrWhiteSpace(id) && id != mijnId)
            .Distinct()
            .ToList();
        if (ledenIds.Count == 0) return BadRequest("Kies minimaal één andere deelnemer.");
        if (ledenIds.Count > 50) return BadRequest("Een groep mag maximaal 50 deelnemers hebben.");

        var leden = await _userManager.Users
            .Where(u => ledenIds.Contains(u.Id) && u.Rol != "Admin")
            .Select(u => new { u.Id, u.Naam })
            .ToListAsync();
        if (leden.Count != ledenIds.Count) return BadRequest("Een of meer gebruikers zijn niet gevonden.");

        // CoachAanbodId = 0: losse groep, niet gekoppeld aan een activiteit
        var groep = new Groepsgesprek
        {
            CoachAanbodId = 0,
            Naam = naam,
            AangemaaktDoorId = mijnId!,
            AangemaaktOp = DateTime.UtcNow
        };
        _context.Groepsgesprekken.Add(groep);
        await _context.SaveChangesAsync();

        var nu = DateTime.UtcNow;
        _context.GroepsgesprekLeden.Add(new GroepsgesprekLid
        {
            GroepsgesprekId = groep.Id, GebruikerId = mijnId!, GebruikerNaam = ik.Naam,
            ToegetreedOp = nu, LaatstGelezen = nu
        });
        foreach (var l in leden)
        {
            _context.GroepsgesprekLeden.Add(new GroepsgesprekLid
            {
                GroepsgesprekId = groep.Id, GebruikerId = l.Id, GebruikerNaam = l.Naam, ToegetreedOp = nu
            });
        }
        _context.Groepsberichten.Add(new Groepsbericht
        {
            GroepsgesprekId = groep.Id,
            VanGebruikerId = mijnId!,
            VanNaam = ik.Naam,
            Tekst = $"👥 {ik.Naam} heeft de groep \"{naam}\" aangemaakt.",
            AangemaaktOp = nu
        });
        await _context.SaveChangesAsync();

        return Ok(new
        {
            GroepsId = groep.Id,
            groep.Naam,
            Leden = new[] { ik.Naam }.Concat(leden.Select(l => l.Naam)).ToList()
        });
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

    [HttpPost("{id}/betaalverzoek")]
    [Authorize(Roles = "Coach")]
    public async Task<IActionResult> StuurBetaalverzoek(int id, [FromBody] GroepsBetaalverzoekVerzoek verzoek)
    {
        var coachId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var coach = await _userManager.FindByIdAsync(coachId!);
        if (coach == null) return Unauthorized();

        if (string.IsNullOrWhiteSpace(verzoek.Omschrijving)) return BadRequest("Omschrijving is verplicht.");
        if (verzoek.Bedrag <= 0) return BadRequest("Bedrag moet groter zijn dan 0.");

        var mijnLid = await _context.GroepsgesprekLeden
            .FirstOrDefaultAsync(l => l.GroepsgesprekId == id && l.GebruikerId == coachId);
        if (mijnLid == null) return Forbid();

        var leden = await _context.GroepsgesprekLeden
            .Where(l => l.GroepsgesprekId == id && l.GebruikerId != coachId)
            .ToListAsync();

        int aantalVerstuurd = 0;
        foreach (var rijderLid in leden)
        {
            _context.Boekingen.Add(new Boeking
            {
                CoachGebruikerId = coachId!,
                RijderGebruikerId = rijderLid.GebruikerId,
                Omschrijving = verzoek.Omschrijving.Trim(),
                Bedrag = verzoek.Bedrag,
                Status = "Openstaand",
                AangemaaktOp = DateTime.UtcNow,
                BetalingsTermijn = verzoek.BetalingsTermijn > 0 ? verzoek.BetalingsTermijn : 14,
                FactuurnummerTekst = verzoek.FactuurnummerTekst,
                FactuurJson = verzoek.FactuurJson
            });
            aantalVerstuurd++;
        }

        _context.Groepsberichten.Add(new Groepsbericht
        {
            GroepsgesprekId = id,
            VanGebruikerId = coachId!,
            VanNaam = coach.Naam,
            Tekst = $"💶 Betaalverzoek verstuurd — {verzoek.Omschrijving.Trim()} — €{verzoek.Bedrag:F2} p.p.\n📲 Elke deelnemer kan betalen via het persoonlijk gesprek.",
            AangemaaktOp = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
        return Ok(new { aantalVerstuurd });
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
public record NieuweGroepVerzoek(string? Naam, List<string>? LedenIds);
public record GroepsBetaalverzoekVerzoek(
    string Omschrijving,
    decimal Bedrag,
    string? FactuurnummerTekst = null,
    int BetalingsTermijn = 14,
    string? FactuurJson = null
);
