using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Backend.Models;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AppDbContext _context;

    public AdminController(UserManager<ApplicationUser> userManager, AppDbContext context)
    {
        _userManager = userManager;
        _context = context;
    }

    [HttpGet("gebruikers")]
    public async Task<IActionResult> GetGebruikers()
    {
        var gebruikers = await _userManager.Users
            .Select(u => new { u.Id, u.Naam, u.Email, u.Rol, u.MarketingToestemming, u.AangemaaktOp, u.AbonnementActief, u.AbonnementVerlooptOp, u.GratisVerlooptOp })
            .ToListAsync();
        return Ok(gebruikers);
    }

    [HttpDelete("gebruikers/{id}")]
    public async Task<IActionResult> VerwijderGebruiker(string id)
    {
        var gebruiker = await _userManager.FindByIdAsync(id);
        if (gebruiker == null) return NotFound();
        if (gebruiker.Email == "admin@mail") return BadRequest("Admin account kan niet verwijderd worden.");

        // Verwijder gekoppeld profiel
        var coach = await _context.Coaches.FirstOrDefaultAsync(c => c.GebruikerId == id);
        if (coach != null) _context.Coaches.Remove(coach);

        var rijder = await _context.Rijders.FirstOrDefaultAsync(r => r.GebruikerId == id);
        if (rijder != null) _context.Rijders.Remove(rijder);

        await _context.SaveChangesAsync();
        await _userManager.DeleteAsync(gebruiker);
        return NoContent();
    }

    // Gratis periode van een coach aanpassen. Beveiligd tegen vergissingen:
    // alleen coaches, max 24 maanden per keer, en de naam van de coach moet
    // als bevestiging worden meegestuurd.
    [HttpPost("gebruikers/{id}/gratis")]
    public async Task<IActionResult> PasGratisAan(string id, [FromBody] GratisAanpassenVerzoek verzoek)
    {
        var gebruiker = await _userManager.FindByIdAsync(id);
        if (gebruiker == null) return NotFound("Gebruiker niet gevonden.");
        if (!gebruiker.Rol.Split(',').Any(r => r.Trim() == "Coach"))
            return BadRequest("Alleen coaches kunnen een gratis periode krijgen.");

        if (!string.Equals((verzoek.Bevestiging ?? "").Trim(), gebruiker.Naam.Trim(), StringComparison.OrdinalIgnoreCase))
            return BadRequest("Bevestiging klopt niet: typ exact de naam van de coach.");

        var nu = DateTime.UtcNow;
        var vorige = gebruiker.GratisVerlooptOp;
        var altijd = new DateTime(9999, 12, 31, 0, 0, 0, DateTimeKind.Utc);

        switch (verzoek.Actie)
        {
            case "maanden":
                if (verzoek.Maanden is null or < 1 or > 24)
                    return BadRequest("Kies tussen 1 en 24 maanden.");
                if (vorige.HasValue && vorige.Value.Year >= 9999)
                    return BadRequest("Deze coach is al voor altijd gratis.");
                // Verleng vanaf de huidige einddatum als die nog loopt, anders vanaf nu
                var basis = vorige.HasValue && vorige.Value > nu ? vorige.Value : nu;
                gebruiker.GratisVerlooptOp = basis.AddMonths(verzoek.Maanden.Value);
                break;
            case "altijd":
                if (vorige.HasValue && vorige.Value.Year >= 9999)
                    return BadRequest("Deze coach is al voor altijd gratis.");
                gebruiker.GratisVerlooptOp = altijd;
                break;
            case "herstel":
                // Alleen bedoeld voor 'Ongedaan maken' direct na een wijziging
                gebruiker.GratisVerlooptOp = verzoek.HerstelNaar;
                break;
            default:
                return BadRequest("Onbekende actie.");
        }

        var resultaat = await _userManager.UpdateAsync(gebruiker);
        if (!resultaat.Succeeded)
            return BadRequest(string.Join(", ", resultaat.Errors.Select(e => e.Description)));

        return Ok(new { vorige, nieuwe = gebruiker.GratisVerlooptOp });
    }

    [HttpGet("coaches")]
    public async Task<IActionResult> GetAlleCoaches()
    {
        var coaches = await _context.Coaches.OrderBy(c => c.Naam).ToListAsync();
        return Ok(coaches);
    }

    [HttpDelete("coaches/{id}")]
    public async Task<IActionResult> VerwijderCoach(int id)
    {
        var coach = await _context.Coaches.FindAsync(id);
        if (coach == null) return NotFound();
        _context.Coaches.Remove(coach);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("rijders")]
    public async Task<IActionResult> GetAlleRijders()
    {
        var rijders = await _context.Rijders.OrderBy(r => r.Naam).ToListAsync();
        return Ok(rijders);
    }

    [HttpDelete("rijders/{id}")]
    public async Task<IActionResult> VerwijderRijder(int id)
    {
        var rijder = await _context.Rijders.FindAsync(id);
        if (rijder == null) return NotFound();
        _context.Rijders.Remove(rijder);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("coach-uitnodiging")]
    public async Task<IActionResult> MaakCoachUitnodiging([FromBody] CoachUitnodigingVerzoek verzoek)
    {
        if (string.IsNullOrWhiteSpace(verzoek.Naam))
            return BadRequest("Naam is verplicht.");

        var code = Random.Shared.Next(10000000, 99999999).ToString();
        var wachtwoord = GenereerWachtwoord();

        var gebruiker = new ApplicationUser
        {
            UserName = code,
            Email = code + "@uitnodiging.racecoachfinder.nl",
            NormalizedEmail = (code + "@uitnodiging.racecoachfinder.nl").ToUpperInvariant(),
            Naam = verzoek.Naam.Trim(),
            Rol = "Coach",
            EmailConfirmed = true,
            HeeftAccountIngericht = false,
            // Voor altijd gratis = einddatum 31-12-9999 (bestaande controles blijven zo werken)
            GratisVerlooptOp = verzoek.VoorAltijdGratis
                ? new DateTime(9999, 12, 31, 0, 0, 0, DateTimeKind.Utc)
                : DateTime.UtcNow.AddMonths(verzoek.GratisMananden),
            AangemaaktOp = DateTime.UtcNow
        };

        var resultaat = await _userManager.CreateAsync(gebruiker, wachtwoord);
        if (!resultaat.Succeeded)
            return BadRequest(string.Join(", ", resultaat.Errors.Select(e => e.Description)));

        return Ok(new { code, wachtwoord, naam = gebruiker.Naam, gratisMananden = verzoek.GratisMananden, voorAltijdGratis = verzoek.VoorAltijdGratis });
    }

    private static string GenereerWachtwoord()
    {
        const string tekens = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghjkmnpqrstuvwxyz23456789";
        return new string(Enumerable.Range(0, 10).Select(_ => tekens[Random.Shared.Next(tekens.Length)]).ToArray());
    }
}

public record GratisAanpassenVerzoek(string Actie, int? Maanden, DateTime? HerstelNaar, string? Bevestiging);
public record CoachUitnodigingVerzoek(string Naam, int GratisMananden = 3, bool VoorAltijdGratis = false);
