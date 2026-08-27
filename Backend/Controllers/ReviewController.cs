using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Backend.Data;
using Backend.Models;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReviewController : ControllerBase
{
    private readonly AppDbContext _context;

    public ReviewController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    [Authorize(Roles = "Rijder")]
    public async Task<IActionResult> MaakReview([FromBody] ReviewVerzoek verzoek)
    {
        var rijderId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (verzoek.Sterren < 1 || verzoek.Sterren > 5)
            return BadRequest("Sterren moet tussen 1 en 5 zijn.");

        var bestaand = await _context.Reviews.AnyAsync(r =>
            r.CoachGebruikerId == verzoek.CoachGebruikerId && r.RijderGebruikerId == rijderId);
        if (bestaand)
            return BadRequest("Je hebt deze coach al beoordeeld.");

        var review = new Review
        {
            CoachGebruikerId = verzoek.CoachGebruikerId,
            RijderGebruikerId = rijderId!,
            Sterren = verzoek.Sterren,
            Tekst = verzoek.Tekst?.Trim() ?? string.Empty,
            AangemaaktOp = DateTime.UtcNow
        };

        _context.Reviews.Add(review);
        await _context.SaveChangesAsync();
        return Ok(review);
    }

    [HttpGet("coach/{coachGebruikerId}")]
    public async Task<IActionResult> GetReviews(string coachGebruikerId)
    {
        var reviews = await _context.Reviews
            .Where(r => r.CoachGebruikerId == coachGebruikerId)
            .OrderByDescending(r => r.AangemaaktOp)
            .Select(r => new
            {
                r.Id,
                r.Sterren,
                r.Tekst,
                r.AangemaaktOp,
                RijderNaam = _context.Users
                    .Where(u => u.Id == r.RijderGebruikerId)
                    .Select(u => u.Naam)
                    .FirstOrDefault() ?? "Rijder"
            })
            .ToListAsync();

        return Ok(reviews);
    }
}

public record ReviewVerzoek(string CoachGebruikerId, int Sterren, string? Tekst);
