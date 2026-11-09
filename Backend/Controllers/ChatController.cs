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
public class ChatController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailService _email;

    public ChatController(AppDbContext context, UserManager<ApplicationUser> userManager, IEmailService email)
    {
        _context = context;
        _userManager = userManager;
        _email = email;
    }

    // Stuur een bericht naar een andere gebruiker
    [HttpPost("{naarGebruikerId}")]
    public async Task<IActionResult> StuurBericht(string naarGebruikerId, [FromBody] BerichtVerzoek verzoek)
    {
        var mijnId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (mijnId == naarGebruikerId) return BadRequest("Je kunt geen bericht naar jezelf sturen.");

        var ontvanger = await _userManager.FindByIdAsync(naarGebruikerId);
        if (ontvanger == null) return NotFound("Gebruiker niet gevonden.");

        if (string.IsNullOrWhiteSpace(verzoek.Tekst))
            return BadRequest("Bericht mag niet leeg zijn.");
        if (verzoek.Tekst.Length > 5000)
            return BadRequest("Je bericht is te lang (maximaal 5000 tekens).");

        // Instellingen van de ontvanger
        var instellingen = await _context.GebruikerInstellingen
            .FirstOrDefaultAsync(i => i.GebruikerId == naarGebruikerId);
        var isFavoriet = await IsFavorietVan(naarGebruikerId, mijnId!);

        // 'Berichten van iedereen' uit: alleen favorieten, of iemand met wie de ontvanger zelf al een gesprek had
        if (instellingen != null && !instellingen.BerichtenVanOnbekenden && !isFavoriet
            && !await _context.Berichten.AnyAsync(b => b.VanGebruikerId == naarGebruikerId && b.NaarGebruikerId == mijnId))
            return StatusCode(403, $"{ontvanger.Naam} ontvangt alleen berichten van favorieten.");

        var bericht = new Bericht
        {
            VanGebruikerId = mijnId!,
            NaarGebruikerId = naarGebruikerId,
            Tekst = verzoek.Tekst.Trim(),
            AangemaaktOp = DateTime.UtcNow,
            Gelezen = false
        };

        _context.Berichten.Add(bericht);
        await _context.SaveChangesAsync();

        // E-mail: niet als de ontvanger alleen mails van favorieten wil en de afzender geen favoriet is
        var emailAan = (instellingen?.EmailAan ?? true) && !(instellingen?.AlleenFavorieten == true && !isFavoriet);
        var drempel = instellingen?.BerichtenDrempel is > 0 ? instellingen.BerichtenDrempel : 1;

        if (emailAan && !string.IsNullOrEmpty(ontvanger.Email))
        {
            // Tel alle ongelezen berichten voor de ontvanger (van alle afzenders)
            var aantalOngelezen = await _context.Berichten
                .CountAsync(b => b.NaarGebruikerId == naarGebruikerId && !b.Gelezen && !b.VerborgenVoorNaar);

            if (aantalOngelezen % drempel == 0)
            {
                var afzender = await _userManager.FindByIdAsync(mijnId!);
                var preview = verzoek.Tekst.Length > 100 ? verzoek.Tekst[..100] + "…" : verzoek.Tekst;
                var url = $"https://www.racecoachfinder.nl/berichten.html?partner={mijnId}&naam={Uri.EscapeDataString(afzender?.Naam ?? "")}&rol={afzender?.Rol ?? ""}";
                var taal = instellingen?.EmailTaal == "en" ? "en" : "nl";
                _ = _email.VerstuurAsync(ontvanger.Email, ontvanger.Naam,
                    EmailTemplates.NieuwBerichtOnderwerp(afzender?.Naam ?? (taal == "en" ? "someone" : "iemand"), taal),
                    EmailTemplates.NieuwBericht(afzender?.Naam ?? (taal == "en" ? "Someone" : "Iemand"), preview, url, taal));
            }
        }

        return Ok(bericht);
    }

    // Heeft 'eigenaarId' 'anderId' als favoriet? (rijder: favoriete coaches, coach: favoriete rijders)
    private async Task<bool> IsFavorietVan(string eigenaarId, string anderId)
    {
        var coachProfielId = await _context.Coaches.Where(c => c.GebruikerId == anderId).Select(c => (int?)c.Id).FirstOrDefaultAsync();
        if (coachProfielId != null && await _context.CoachFavorieten.AnyAsync(f => f.RijderId == eigenaarId && f.CoachId == coachProfielId))
            return true;
        return await _context.RijderFavorieten.AnyAsync(f => f.CoachId == eigenaarId && f.RijderId == anderId);
    }

    // Haal alle berichten op in een gesprek met een specifieke gebruiker
    [HttpGet("gesprek/{partnerId}")]
    public async Task<IActionResult> GetGesprek(string partnerId)
    {
        var mijnId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var berichten = await _context.Berichten
            .Where(b =>
                (b.VanGebruikerId == mijnId && b.NaarGebruikerId == partnerId && !b.VerborgenVoorVan) ||
                (b.VanGebruikerId == partnerId && b.NaarGebruikerId == mijnId && !b.VerborgenVoorNaar))
            .OrderBy(b => b.AangemaaktOp)
            .ToListAsync();

        // Markeer ontvangen berichten als gelezen
        var ongelezen = berichten.Where(b => b.NaarGebruikerId == mijnId && !b.Gelezen).ToList();
        ongelezen.ForEach(b => b.Gelezen = true);
        if (ongelezen.Any()) await _context.SaveChangesAsync();

        return Ok(berichten);
    }

    // Haal inbox op: lijst van gesprekken met laatste bericht
    [HttpGet("gesprekken")]
    public async Task<IActionResult> GetGesprekken()
    {
        var mijnId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var berichten = await _context.Berichten
            .Where(b => (b.VanGebruikerId == mijnId && !b.VerborgenVoorVan) || (b.NaarGebruikerId == mijnId && !b.VerborgenVoorNaar))
            .OrderByDescending(b => b.AangemaaktOp)
            .ToListAsync();

        var gesprekken = berichten
            .GroupBy(b => b.VanGebruikerId == mijnId ? b.NaarGebruikerId : b.VanGebruikerId)
            .Select(g => new
            {
                PartnerId = g.Key,
                LaatsteBericht = g.First().Tekst,
                LaatsteTijd = g.First().AangemaaktOp,
                AantalOngelezen = g.Count(b => b.NaarGebruikerId == mijnId && !b.Gelezen)
            })
            .ToList();

        var partnerIds = gesprekken.Select(g => g.PartnerId).ToList();
        var partners = await _userManager.Users
            .Where(u => partnerIds.Contains(u.Id))
            .Select(u => new { u.Id, u.Naam, u.Rol })
            .ToListAsync();

        var coachFotos = await _context.Coaches
            .Where(c => c.GebruikerId != null && partnerIds.Contains(c.GebruikerId))
            .ToDictionaryAsync(c => c.GebruikerId!, c => c.FotoUrl);

        var rijderFotos = await _context.Rijders
            .Where(r => r.GebruikerId != null && partnerIds.Contains(r.GebruikerId))
            .ToDictionaryAsync(r => r.GebruikerId!, r => r.FotoUrl);

        var resultaat = gesprekken.Select(g =>
        {
            var partner = partners.FirstOrDefault(p => p.Id == g.PartnerId);
            var fotoUrl = coachFotos.GetValueOrDefault(g.PartnerId)
                       ?? rijderFotos.GetValueOrDefault(g.PartnerId)
                       ?? "";
            return new
            {
                g.PartnerId,
                PartnerNaam = partner?.Naam ?? "Onbekend",
                PartnerRol = partner?.Rol ?? "",
                PartnerFotoUrl = fotoUrl,
                g.LaatsteBericht,
                g.LaatsteTijd,
                g.AantalOngelezen
            };
        }).ToList();

        return Ok(resultaat);
    }

    // Verwijder een gesprek: alleen voor de huidige gebruiker (de ander houdt het gesprek)
    [HttpDelete("gesprek/{partnerId}")]
    public async Task<IActionResult> VerwijderGesprek(string partnerId)
    {
        var mijnId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var berichten = await _context.Berichten
            .Where(b =>
                (b.VanGebruikerId == mijnId && b.NaarGebruikerId == partnerId) ||
                (b.VanGebruikerId == partnerId && b.NaarGebruikerId == mijnId))
            .ToListAsync();
        foreach (var b in berichten)
        {
            if (b.VanGebruikerId == mijnId) b.VerborgenVoorVan = true;
            else { b.VerborgenVoorNaar = true; b.Gelezen = true; }
        }
        // Alleen echt weggooien als beide kanten het gesprek hebben verwijderd
        _context.Berichten.RemoveRange(berichten.Where(b => b.VerborgenVoorVan && b.VerborgenVoorNaar));
        await _context.SaveChangesAsync();
        return NoContent();
    }

    // Zoek gebruikers op naam om een nieuw gesprek mee te starten
    [HttpGet("zoek")]
    public async Task<IActionResult> ZoekGebruikers([FromQuery] string? q)
    {
        var mijnId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var zoekterm = (q ?? "").Trim().Replace("%", "").Replace("_", "");
        if (zoekterm.Length < 2) return Ok(Array.Empty<object>());

        var gebruikers = await _userManager.Users
            .Where(u => u.Id != mijnId && u.Rol != "Admin" && EF.Functions.Like(u.Naam, $"%{zoekterm}%")
                && !_context.GebruikerInstellingen.Any(i => i.GebruikerId == u.Id && !i.ProfielOpenbaar))
            .OrderBy(u => u.Naam)
            .Take(20)
            .Select(u => new { u.Id, u.Naam, u.Rol })
            .ToListAsync();

        var ids = gebruikers.Select(g => g.Id).ToList();
        var coachFotos = await _context.Coaches
            .Where(c => c.GebruikerId != null && ids.Contains(c.GebruikerId))
            .ToDictionaryAsync(c => c.GebruikerId!, c => c.FotoUrl);
        var rijderFotos = await _context.Rijders
            .Where(r => r.GebruikerId != null && ids.Contains(r.GebruikerId))
            .ToDictionaryAsync(r => r.GebruikerId!, r => r.FotoUrl);

        return Ok(gebruikers.Select(g => new
        {
            g.Id,
            g.Naam,
            g.Rol,
            FotoUrl = coachFotos.GetValueOrDefault(g.Id) ?? rijderFotos.GetValueOrDefault(g.Id) ?? ""
        }));
    }

    // Verwijder één eigen bericht
    [HttpDelete("bericht/{id:int}")]
    public async Task<IActionResult> VerwijderBericht(int id)
    {
        var mijnId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var bericht = await _context.Berichten.FindAsync(id);
        if (bericht == null) return NotFound();
        if (bericht.VanGebruikerId != mijnId) return Forbid();

        _context.Berichten.Remove(bericht);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    // Bewerk één eigen bericht
    [HttpPut("bericht/{id:int}")]
    public async Task<IActionResult> BewerkBericht(int id, [FromBody] BerichtVerzoek verzoek)
    {
        var mijnId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var bericht = await _context.Berichten.FindAsync(id);
        if (bericht == null) return NotFound();
        if (bericht.VanGebruikerId != mijnId) return Forbid();
        if (string.IsNullOrWhiteSpace(verzoek.Tekst)) return BadRequest("Bericht mag niet leeg zijn.");

        bericht.Tekst = verzoek.Tekst.Trim();
        bericht.BewerktOp = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return Ok(bericht);
    }

    // Aantal ongelezen berichten (voor nav badge)
    [HttpGet("ongelezen")]
    public async Task<IActionResult> GetAantalOngelezen()
    {
        var mijnId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var aantal = await _context.Berichten
            .CountAsync(b => b.NaarGebruikerId == mijnId && !b.Gelezen && !b.VerborgenVoorNaar);
        return Ok(aantal);
    }
}

public record BerichtVerzoek(string Tekst);
