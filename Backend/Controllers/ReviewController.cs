using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Backend.Data;
using Backend.Models;
using Backend.Services;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReviewController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IEmailService _email;
    private readonly ILogger<ReviewController> _logger;

    public ReviewController(AppDbContext context, IEmailService email, ILogger<ReviewController> logger)
    {
        _context = context;
        _email = email;
        _logger = logger;
    }

    [HttpPost]
    [Authorize(Roles = "Rijder")]
    public async Task<IActionResult> MaakReview([FromBody] ReviewVerzoek verzoek)
    {
        var rijderId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (verzoek.Sterren < 1 || verzoek.Sterren > 5)
            return BadRequest("Sterren moet tussen 1 en 5 zijn.");
        if (verzoek.CoachGebruikerId == rijderId)
            return BadRequest("Je kunt jezelf niet beoordelen.");
        var coach = await _context.Users.FirstOrDefaultAsync(u => u.Id == verzoek.CoachGebruikerId);
        if (coach == null || !(coach.Rol ?? "").Split(',').Any(r => r.Trim() == "Coach"))
            return BadRequest("Coach niet gevonden.");
        if ((verzoek.Tekst ?? "").Length > 2000)
            return BadRequest("Je review mag maximaal 2000 tekens zijn.");
        if (!await MagReviewen(rijderId!, verzoek.CoachGebruikerId))
            return BadRequest("Je kunt een review schrijven nadat je een coaching met deze coach hebt betaald en als gedaan hebt bevestigd.");

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

        // Mail naar de coach (instelling 'Nieuwe beoordeling')
        var inst = await _context.GebruikerInstellingen.FirstOrDefaultAsync(i => i.GebruikerId == coach.Id);
        if ((inst?.EmailAan ?? true) && (inst?.EmailBijNieuweReview ?? true) && !string.IsNullOrEmpty(coach.Email) && coach.Email.Contains('@'))
        {
            var rijderNaam = await _context.Users.Where(u => u.Id == rijderId).Select(u => u.Naam).FirstOrDefaultAsync() ?? "Een rijder";
            var profielId = await _context.Coaches.Where(c => c.GebruikerId == coach.Id).Select(c => (int?)c.Id).FirstOrDefaultAsync();
            var url = profielId != null ? $"https://www.racecoachfinder.nl/coach-detail.html?id={profielId}" : "https://www.racecoachfinder.nl/dashboard-coach.html";
            var taal = inst?.EmailTaal == "en" ? "en" : "nl";
            _ = _email.VerstuurAsync(coach.Email, coach.Naam, EmailTemplates.ReviewOntvangenOnderwerp(rijderNaam, taal),
                    EmailTemplates.ReviewOntvangen(coach.Naam, rijderNaam, review.Sterren, review.Tekst, url, taal))
                .ContinueWith(t => _logger.LogWarning("Review-mail mislukt: {Fout}", t.Exception?.GetBaseException().Message),
                    TaskContinuationOptions.OnlyOnFaulted);
        }
        return Ok(review);
    }

    // Review mag alleen na een betaalde boeking die de rijder als gedaan heeft bevestigd
    private Task<bool> MagReviewen(string rijderId, string coachId) =>
        _context.Boekingen.AnyAsync(b => b.RijderGebruikerId == rijderId && b.CoachGebruikerId == coachId
            && (b.Status == "Voldaan" || (b.Status == "Betaald" && b.RijderHeeftBevestigd)));

    // Voor de coachpagina: mag ik deze coach (nog) beoordelen?
    [HttpGet("mag/{coachGebruikerId}")]
    [Authorize]
    public async Task<IActionResult> Mag(string coachGebruikerId)
    {
        var mijnId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var alGeschreven = await _context.Reviews.AnyAsync(r => r.CoachGebruikerId == coachGebruikerId && r.RijderGebruikerId == mijnId);
        var heeftBoeking = await MagReviewen(mijnId, coachGebruikerId);
        return Ok(new { mag = heeftBoeking && !alGeschreven && mijnId != coachGebruikerId, alGeschreven, heeftBoeking });
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
                r.RijderGebruikerId,
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
