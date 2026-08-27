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
            .Select(u => new { u.Id, u.Naam, u.Email, u.Rol })
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
            HeeftAccountIngericht = false
        };

        var resultaat = await _userManager.CreateAsync(gebruiker, wachtwoord);
        if (!resultaat.Succeeded)
            return BadRequest(string.Join(", ", resultaat.Errors.Select(e => e.Description)));

        return Ok(new { code, wachtwoord, naam = gebruiker.Naam });
    }

    private static string GenereerWachtwoord()
    {
        const string tekens = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghjkmnpqrstuvwxyz23456789";
        return new string(Enumerable.Range(0, 10).Select(_ => tekens[Random.Shared.Next(tekens.Length)]).ToArray());
    }
}

public record CoachUitnodigingVerzoek(string Naam);
