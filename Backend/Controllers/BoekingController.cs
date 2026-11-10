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
    private const string FrontendUrl = "https://www.racecoachfinder.nl";

    public BoekingController(AppDbContext context, UserManager<ApplicationUser> userManager, IEmailService email)
    {
        _context = context;
        _userManager = userManager;
        _email = email;
    }

    // Volgende vrije factuurnummer van deze coach (doorlopend per jaar), om in het factuurformulier te zetten
    [HttpGet("volgend-factuurnummer")]
    [Authorize(Roles = "Coach")]
    public async Task<IActionResult> VolgendFactuurnummer([FromServices] FactuurService facturen)
    {
        var coachId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        return Ok(new { nummer = await facturen.VolgendNummerAsync(coachId) });
    }

    // Coach stuurt betaalverzoek/factuur naar rijder
    [HttpPost]
    [Authorize(Roles = "Coach")]
    public async Task<IActionResult> MaakBoeking([FromBody] BoekingVerzoek verzoek, [FromServices] FactuurService facturen, [FromServices] MollieConnectService connect)
    {
        var coachId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var coach = await _userManager.FindByIdAsync(coachId!);
        if (coach == null) return Unauthorized();
        var rijder = await _userManager.FindByIdAsync(verzoek.RijderGebruikerId);
        if (rijder == null) return NotFound("Rijder niet gevonden.");
        if (rijder.Id == coach.Id) return BadRequest("Je kunt geen factuur naar jezelf sturen.");
        if (string.IsNullOrWhiteSpace(verzoek.Omschrijving)) return BadRequest("Omschrijving is verplicht.");
        if (verzoek.Bedrag <= 0) return BadRequest("Bedrag moet groter zijn dan 0.");

        // Hoe betaalt de rijder? Online via het eigen Mollie-account van de coach, of (zonder Mollie) via overmaken
        var (betaalKeuze, geenBetaalWijze) = await connect.BetaalKeuzeAsync(coach.Id);
        if (betaalKeuze == null) return BadRequest(geenBetaalWijze);
        var betaalLink = FactuurService.ControleerBetaalLink(verzoek.BetaalLink);
        if (betaalLink == "") return BadRequest("De betaalverzoek-link klopt niet. Plak de volledige link (die begint met https://).");

        // Factuurnummer: zelf ingevuld (mag niet al gebruikt zijn) of het volgende doorlopende nummer
        var nummer = (verzoek.FactuurnummerTekst ?? "").Trim();
        if (nummer.Length > 40) return BadRequest("Factuurnummer is te lang.");
        if (nummer == "") nummer = await facturen.VolgendNummerAsync(coach.Id);
        else if (await facturen.NummerAlGebruiktAsync(coach.Id, nummer))
            return BadRequest($"Factuurnummer {nummer} heb je al gebruikt. Elke factuur moet een eigen nummer hebben.");

        var (boeking, emailFout) = await facturen.MaakEnVerstuurAsync(coach, rijder, verzoek.Omschrijving, verzoek.Bedrag,
            nummer, verzoek.BetalingsTermijn, verzoek.FactuurJson, betaalKeuze, betaalLink);

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
            boeking.BetaalWijze,
            emailFout
        });
    }

    // Haal één boeking op (voor betaalscherm)
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetBoeking(int id, [FromServices] MollieConnectService connect)
    {
        var mijnId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var boeking = await _context.Boekingen.FindAsync(id);
        if (boeking == null) return NotFound();
        if (boeking.CoachGebruikerId != mijnId && boeking.RijderGebruikerId != mijnId) return Forbid();

        var coach = await _userManager.FindByIdAsync(boeking.CoachGebruikerId);
        var overmaken = boeking.BetaalWijze == BetaalKeuze.Overmaken;
        return Ok(new
        {
            boeking.Id,
            boeking.Omschrijving,
            boeking.Bedrag,
            boeking.Status,
            boeking.FactuurnummerTekst,
            boeking.BetalingsTermijn,
            boeking.AangemaaktOp,
            CoachNaam = coach?.Naam ?? "Coach",
            CoachId = boeking.CoachGebruikerId,
            BetaalWijze = overmaken ? BetaalKeuze.Overmaken : BetaalKeuze.Online,
            Servicekosten = overmaken ? 0 : MollieConnectService.Servicekosten(boeking.Bedrag),
            KanOnlineBetalen = !overmaken && (boeking.Status != "Openstaand" || await connect.StatusVoorAsync(boeking.CoachGebruikerId) == KoppelStatus.Klaar),
            Iban = overmaken ? IbanHelper.Opmaak(boeking.Iban) : null,
            boeking.IbanTenaamstelling,
            boeking.BetaalLink,
            boeking.RijderMeldtBetaaldOp,
            ZakelijkGeverifieerd = (await connect.ZakelijkGeverifieerdAsync(new[] { boeking.CoachGebruikerId })).Count > 0
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
        if (boeking.Status != "Betaald")
            return BadRequest("Bevestigen kan pas als er betaald is.");

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

    // Overmaken: rijder geeft aan dat hij betaald heeft. De factuur blijft open tot de coach "Ontvangen" klikt.
    [HttpPost("{id:int}/betaald-gemeld")]
    public async Task<IActionResult> BetaaldGemeld(int id)
    {
        var mijnId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var boeking = await _context.Boekingen.FindAsync(id);
        if (boeking == null) return NotFound();
        if (boeking.RijderGebruikerId != mijnId) return Forbid();
        if (boeking.BetaalWijze != BetaalKeuze.Overmaken) return BadRequest("Deze factuur betaal je online.");
        if (boeking.Status != "Openstaand") return BadRequest("Deze factuur staat niet meer open.");
        if (boeking.RijderMeldtBetaaldOp != null) return Ok(new { boeking.RijderMeldtBetaaldOp });

        boeking.RijderMeldtBetaaldOp = DateTime.UtcNow;
        var nummer = Factuurnummer(boeking);
        _context.Berichten.Add(new Bericht
        {
            VanGebruikerId = boeking.RijderGebruikerId,
            NaarGebruikerId = boeking.CoachGebruikerId,
            Tekst = $"💶 Ik heb factuur {nummer} ({EuroNl(boeking.Bedrag)}) overgemaakt.",
            AangemaaktOp = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Mail naar de coach (instelling 'Betaalverzoek updates')
        try
        {
            var coach = await _userManager.FindByIdAsync(boeking.CoachGebruikerId);
            var rijder = await _userManager.FindByIdAsync(boeking.RijderGebruikerId);
            var inst = await _context.GebruikerInstellingen.FirstOrDefaultAsync(i => i.GebruikerId == boeking.CoachGebruikerId);
            if (coach?.Email?.Contains('@') == true && (inst?.EmailAan ?? true) && (inst?.EmailBijBetaalUpdate ?? true))
            {
                var taal = inst?.EmailTaal == "en" ? "en" : "nl";
                await _email.VerstuurAsync(coach.Email, coach.Naam, EmailTemplates.BetalingGemeldOnderwerp(nummer, rijder?.Naam ?? "Rijder", taal),
                    EmailTemplates.BetalingGemeld(coach.Naam, rijder?.Naam ?? "Rijder", nummer, boeking.Bedrag,
                        $"{FrontendUrl}/berichten.html?partner={Uri.EscapeDataString(boeking.RijderGebruikerId)}", taal));
            }
        }
        catch (Exception ex) { Console.Error.WriteLine("Mail betaling gemeld mislukt: " + ex.Message); }

        return Ok(new { boeking.RijderMeldtBetaaldOp });
    }

    // Overmaken: coach bevestigt dat het geld op zijn rekening staat → factuur betaald
    [HttpPost("{id:int}/ontvangen")]
    [Authorize(Roles = "Coach")]
    public async Task<IActionResult> Ontvangen(int id)
    {
        var mijnId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var boeking = await _context.Boekingen.FindAsync(id);
        if (boeking == null) return NotFound();
        if (boeking.CoachGebruikerId != mijnId) return Forbid();
        if (boeking.BetaalWijze != BetaalKeuze.Overmaken) return BadRequest("Deze factuur wordt online betaald; dat wordt automatisch bijgewerkt.");
        if (boeking.Status == "Betaald" || boeking.Status == "Voldaan") return Ok(boeking);
        if (boeking.Status != "Openstaand") return BadRequest("Deze factuur staat niet meer open.");

        boeking.Status = "Betaald";
        var nummer = Factuurnummer(boeking);
        _context.Berichten.Add(new Bericht
        {
            VanGebruikerId = boeking.CoachGebruikerId,
            NaarGebruikerId = boeking.RijderGebruikerId,
            Tekst = $"✅ Betaling voor factuur {nummer} ontvangen. Bedankt!",
            AangemaaktOp = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Mail naar de rijder
        try
        {
            var coach = await _userManager.FindByIdAsync(boeking.CoachGebruikerId);
            var rijder = await _userManager.FindByIdAsync(boeking.RijderGebruikerId);
            var inst = await _context.GebruikerInstellingen.FirstOrDefaultAsync(i => i.GebruikerId == boeking.RijderGebruikerId);
            if (rijder?.Email?.Contains('@') == true && (inst?.EmailAan ?? true))
            {
                var taal = inst?.EmailTaal == "en" ? "en" : "nl";
                await _email.VerstuurAsync(rijder.Email, rijder.Naam, EmailTemplates.BetalingOntvangenOnderwerp(nummer, coach?.Naam ?? "Coach", taal),
                    EmailTemplates.BetalingOntvangen(rijder.Naam, coach?.Naam ?? "Coach", nummer, boeking.Bedrag,
                        $"{FrontendUrl}/berichten.html?partner={Uri.EscapeDataString(boeking.CoachGebruikerId)}", taal));
            }
        }
        catch (Exception ex) { Console.Error.WriteLine("Mail betaling ontvangen mislukt: " + ex.Message); }

        return Ok(boeking);
    }

    private static string Factuurnummer(Boeking b) => b.FactuurnummerTekst ?? $"F-{b.AangemaaktOp:yyyy}-{b.Id:D4}";
    private static string EuroNl(decimal bedrag) => "€ " + bedrag.ToString("N2", new System.Globalization.CultureInfo("nl-NL"));

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
    string? FactuurJson,
    string? BetaalLink = null
);
