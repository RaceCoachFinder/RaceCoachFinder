using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Backend.Data;
using Backend.Models;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RijderController : ControllerBase
{
    private readonly AppDbContext _context;

    public RijderController(AppDbContext context)
    {
        _context = context;
    }

    // Publiek: aantal rijders voor homepage stats
    [HttpGet("aantal")]
    public async Task<IActionResult> GetAantal()
    {
        var aantal = await _context.Rijders.CountAsync();
        return Ok(aantal);
    }

    // Publiek: rijderprofiel ophalen via ApplicationUser ID
    [HttpGet("profiel/{gebruikerId}")]
    public async Task<IActionResult> GetProfiel(string gebruikerId)
    {
        var rijder = await _context.Rijders.FirstOrDefaultAsync(r => r.GebruikerId == gebruikerId);
        if (rijder == null) return NotFound();
        return Ok(rijder);
    }

    // Rijder: eigen profiel ophalen
    [HttpGet("mijn")]
    [Authorize(Roles = "Rijder")]
    public async Task<ActionResult<Rijder>> GetMijnProfiel()
    {
        var gebruikerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var rijder = await _context.Rijders.FirstOrDefaultAsync(r => r.GebruikerId == gebruikerId);
        if (rijder == null) return NotFound();
        return rijder;
    }

    // Rijder: profiel aanmaken
    [HttpPost]
    [Authorize(Roles = "Rijder")]
    public async Task<ActionResult<Rijder>> PostRijder(Rijder rijder)
    {
        var gebruikerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (await _context.Rijders.AnyAsync(r => r.GebruikerId == gebruikerId))
            return BadRequest("Je hebt al een rijderprofiel.");

        rijder.GebruikerId = gebruikerId;
        _context.Rijders.Add(rijder);
        await _context.SaveChangesAsync();
        return Ok(rijder);
    }

    // Rijder: eigen profiel bijwerken
    [HttpPut("mijn")]
    [Authorize(Roles = "Rijder")]
    public async Task<IActionResult> PutMijnProfiel(Rijder rijder)
    {
        var gebruikerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var bestaand = await _context.Rijders.FirstOrDefaultAsync(r => r.GebruikerId == gebruikerId);
        if (bestaand == null) return NotFound();

        bestaand.Naam = rijder.Naam;
        bestaand.Email = rijder.Email;
        bestaand.Woonplaats = rijder.Woonplaats;
        bestaand.Nationaliteit = rijder.Nationaliteit;
        bestaand.Talen = rijder.Talen;
        bestaand.Bio = rijder.Bio;
        bestaand.Leeftijd = rijder.Leeftijd;
        bestaand.Niveau = rijder.Niveau;
        bestaand.Discipline = rijder.Discipline;
        bestaand.Doelen = rijder.Doelen;
        bestaand.BeschikbareBanen = rijder.BeschikbareBanen;
        bestaand.Instagram = rijder.Instagram;
        bestaand.Resultaten = rijder.Resultaten;
        if (!string.IsNullOrEmpty(rijder.FotoUrl)) bestaand.FotoUrl = rijder.FotoUrl;

        await _context.SaveChangesAsync();
        return Ok(bestaand);
    }
}
