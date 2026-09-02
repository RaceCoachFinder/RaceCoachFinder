using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Backend.Data;
using Backend.Models;
using Backend.Services;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BoekingController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailService _email;
    private const string FrontendUrl = "https://racecoachfinder.netlify.app";

    public BoekingController(AppDbContext context, UserManager<ApplicationUser> userManager, IEmailService email)
    {
        _context = context;
        _userManager = userManager;
        _email = email;
    }

    // Coach stuurt betaalverzoek/factuur naar rijder
    [HttpPost]
    [Authorize(Roles = "Coach")]
    public async Task<IActionResult> MaakBoeking([FromBody] BoekingVerzoek verzoek)
    {
        var coachId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var coach = await _userManager.FindByIdAsync(coachId!);
        var rijder = await _userManager.FindByIdAsync(verzoek.RijderGebruikerId);
        if (rijder == null) return NotFound("Rijder niet gevonden.");
        if (string.IsNullOrWhiteSpace(verzoek.Omschrijving)) return BadRequest("Omschrijving is verplicht.");
        if (verzoek.Bedrag <= 0) return BadRequest("Bedrag moet groter zijn dan 0.");

        var boeking = new Boeking
        {
            CoachGebruikerId = coachId!,
            RijderGebruikerId = verzoek.RijderGebruikerId,
            Omschrijving = verzoek.Omschrijving.Trim(),
            Bedrag = verzoek.Bedrag,
            Status = "Openstaand",
            AangemaaktOp = DateTime.UtcNow,
            FactuurnummerTekst = verzoek.FactuurnummerTekst,
            BetalingsTermijn = verzoek.BetalingsTermijn > 0 ? verzoek.BetalingsTermijn : 14,
            FactuurJson = verzoek.FactuurJson
        };

        _context.Boekingen.Add(boeking);
        await _context.SaveChangesAsync();

        var coachNaam = coach?.Naam ?? "Coach";
        var rijderNaam = rijder.Naam ?? "Rijder";
        var factuurnummer = verzoek.FactuurnummerTekst ?? $"F-{DateTime.UtcNow:yyyy}-{boeking.Id:D4}";
        var berichtenUrl = $"{FrontendUrl}/berichten.html?partner={coachId}";

        // PDF genereren
        byte[]? pdfBytes = null;
        var emailFout = "";
        try
        {
            var coachProfiel = await _context.Coaches.FirstOrDefaultAsync(c => c.GebruikerId == coachId);
            var factuurData = PdfService.ParseFactuurJson(verzoek.FactuurJson);
            var regels = factuurData?.Regels ?? new List<FactuurRegel>
            {
                new() { Omschrijving = verzoek.Omschrijving, Aantal = 1, Prijs = (double)verzoek.Bedrag, Btw = 0 }
            };

            var (pdf, totaalMetFee) = PdfService.GenereerFactuur(
                factuurnummer,
                DateTime.Now,
                boeking.BetalingsTermijn,
                coachNaam,
                coachProfiel?.FactuurAdres,
                coachProfiel?.FactuurPostcode,
                coachProfiel?.FactuurStad,
                coachProfiel?.FactuurLand,
                coachProfiel?.FactuurTelefoon,
                coach?.Email ?? coachNaam,
                coachProfiel?.KvkNummer,
                coachProfiel?.BtwNummer,
                rijderNaam,
                rijder.Email ?? rijderNaam,
                regels,
                factuurData?.Notities);

            pdfBytes = pdf;
            boeking.Bedrag = totaalMetFee;
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            emailFout = $"PDF mislukt: {ex.Message}";
        }

        var pdfNaam = $"Factuur-{factuurnummer}.pdf";

        var coachEmail = coach?.Email ?? "";
        var geldigCoachEmail = coachEmail.Contains('@') && coachEmail.IndexOf('.', coachEmail.IndexOf('@')) > 0;
        if (geldigCoachEmail)
        {
            try
            {
                await _email.VerstuurAsync(
                    coachEmail, coachNaam, $"Factuur {factuurnummer} verstuurd",
                    EmailTemplates.FactuurCoach(coachNaam, rijderNaam, factuurnummer, verzoek.Omschrijving, boeking.Bedrag),
                    pdfBytes, pdfNaam);
            }
            catch (Exception ex)
            {
                emailFout += (emailFout.Length > 0 ? " | " : "") + $"Coach email mislukt: {ex.Message}";
            }
        }

        if (!string.IsNullOrEmpty(rijder.Email) && rijder.Email.Contains('@') && rijder.Email.IndexOf('.', rijder.Email.IndexOf('@')) > 0)
        {
            try
            {
                await _email.VerstuurAsync(
                    rijder.Email, rijderNaam, $"Factuur {factuurnummer} van {coachNaam}",
                    EmailTemplates.FactuurRijder(rijderNaam, coachNaam, factuurnummer, verzoek.Omschrijving, boeking.Bedrag, boeking.BetalingsTermijn, berichtenUrl),
                    pdfBytes, pdfNaam);
            }
            catch (Exception ex)
            {
                emailFout += (emailFout.Length > 0 ? " | " : "") + $"Rijder email mislukt: {ex.Message}";
            }
        }

        return Ok(new
        {
            boeking.Id,
            boeking.CoachGebruikerId,
            boeking.RijderGebruikerId,
            boeking.Omschrijving,
            boeking.Bedrag,
            boeking.Status,
            boeking.AangemaaktOp,
            boeking.FactuurnummerTekst,
            boeking.BetalingsTermijn,
            emailFout
        });
    }

    // Haal boekingen op voor gesprek tussen huidige gebruiker en partner
    [HttpGet("gesprek/{partnerId}")]
    public async Task<IActionResult> GetBoekingen(string partnerId)
    {
        var mijnId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var boekingen = await _context.Boekingen
            .Where(b =>
                (b.CoachGebruikerId == mijnId && b.RijderGebruikerId == partnerId) ||
                (b.CoachGebruikerId == partnerId && b.RijderGebruikerId == mijnId))
            .OrderBy(b => b.AangemaaktOp)
            .ToListAsync();
        return Ok(boekingen);
    }

    // Rijder markeert als betaald
    [HttpPut("{id}/betaal")]
    [Authorize(Roles = "Rijder")]
    public async Task<IActionResult> Betaal(int id)
    {
        var rijderId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var boeking = await _context.Boekingen.FindAsync(id);
        if (boeking == null || boeking.RijderGebruikerId != rijderId) return NotFound();
        if (boeking.Status != "Openstaand") return BadRequest("Boeking is niet meer openstaand.");

        boeking.Status = "Betaald";
        await _context.SaveChangesAsync();
        return Ok(boeking);
    }

    // Activiteit bevestigen (coach of rijder) — beide moeten bevestigen → "Voldaan"
    [HttpPut("{id}/bevestig")]
    public async Task<IActionResult> Bevestig(int id)
    {
        var mijnId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var boeking = await _context.Boekingen.FindAsync(id);
        if (boeking == null) return NotFound();
        if (boeking.CoachGebruikerId != mijnId && boeking.RijderGebruikerId != mijnId)
            return Forbid();
        if (boeking.Status == "Geannuleerd" || boeking.Status == "Voldaan")
            return BadRequest("Boeking is al afgerond.");

        if (boeking.CoachGebruikerId == mijnId)
            boeking.CoachHeeftBevestigd = true;
        else
            boeking.RijderHeeftBevestigd = true;

        if (boeking.CoachHeeftBevestigd && boeking.RijderHeeftBevestigd)
            boeking.Status = "Voldaan";

        await _context.SaveChangesAsync();
        return Ok(boeking);
    }

    // Overzicht van alle boekingen voor de ingelogde coach
    [HttpGet("coach-overzicht")]
    [Authorize(Roles = "Coach")]
    public async Task<IActionResult> CoachOverzicht()
    {
        var coachId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var boekingen = await _context.Boekingen
            .Where(b => b.CoachGebruikerId == coachId && b.Status != "Geannuleerd")
            .OrderByDescending(b => b.AangemaaktOp)
            .ToListAsync();

        var rijderIds = boekingen.Select(b => b.RijderGebruikerId).Distinct().ToList();
        var rijders = await _context.Users
            .Where(u => rijderIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Naam);

        var resultaat = boekingen.Select(b => new
        {
            b.Id,
            b.Omschrijving,
            b.Bedrag,
            b.Status,
            b.AangemaaktOp,
            b.CoachHeeftBevestigd,
            b.RijderHeeftBevestigd,
            RijderNaam = rijders.GetValueOrDefault(b.RijderGebruikerId, "Onbekend"),
            DisplayStatus = BerekenDisplayStatus(b)
        });

        return Ok(resultaat);
    }

    private static string BerekenDisplayStatus(Boeking b) =>
        (b.Status, b.CoachHeeftBevestigd, b.RijderHeeftBevestigd) switch
        {
            ("Openstaand", _, _) => "Nog niet betaald",
            ("Betaald", false, false) => "Beide nog niet geaccepteerd",
            ("Betaald", true, false) => "Rijder nog niet geaccepteerd",
            ("Betaald", false, true) => "Coach niet geaccepteerd",
            ("Voldaan", _, _) => "Volledig voldaan",
            _ => b.Status
        };

    // Overzicht van alle boekingen voor de ingelogde rijder
    [HttpGet("rijder-overzicht")]
    [Authorize(Roles = "Rijder")]
    public async Task<IActionResult> RijderOverzicht()
    {
        var rijderId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var boekingen = await _context.Boekingen
            .Where(b => b.RijderGebruikerId == rijderId && b.Status != "Geannuleerd")
            .OrderByDescending(b => b.AangemaaktOp)
            .ToListAsync();

        var coachIds = boekingen.Select(b => b.CoachGebruikerId).Distinct().ToList();
        var coaches = await _context.Users
            .Where(u => coachIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Naam);

        var coachProfielen = await _context.Coaches
            .Where(c => coachIds.Contains(c.GebruikerId!))
            .ToDictionaryAsync(c => c.GebruikerId!, c => c.Id);

        var resultaat = boekingen.Select(b => new
        {
            b.Id,
            b.Omschrijving,
            b.Bedrag,
            b.Status,
            b.AangemaaktOp,
            b.CoachHeeftBevestigd,
            b.RijderHeeftBevestigd,
            CoachNaam = coaches.GetValueOrDefault(b.CoachGebruikerId, "Onbekend"),
            CoachProfielId = coachProfielen.GetValueOrDefault(b.CoachGebruikerId, 0),
            DisplayStatus = BerekenDisplayStatus(b)
        });

        return Ok(resultaat);
    }

    // Annuleer boeking (coach of rijder)
    [HttpPut("{id}/annuleer")]
    public async Task<IActionResult> Annuleer(int id)
    {
        var mijnId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var boeking = await _context.Boekingen.FindAsync(id);
        if (boeking == null) return NotFound();
        if (boeking.CoachGebruikerId != mijnId && boeking.RijderGebruikerId != mijnId)
            return Forbid();
        if (boeking.Status != "Openstaand") return BadRequest("Boeking is niet meer openstaand.");

        boeking.Status = "Geannuleerd";
        await _context.SaveChangesAsync();
        return Ok(boeking);
    }
}

public record BoekingVerzoek(
    string RijderGebruikerId,
    string Omschrijving,
    decimal Bedrag,
    string? FactuurnummerTekst,
    int BetalingsTermijn,
    string? FactuurJson
);
