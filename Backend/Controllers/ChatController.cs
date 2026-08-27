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

        // Stuur e-mailmelding alleen als dit de eerste ongelezen bericht van deze afzender is
        var heeftAlOngelezen = await _context.Berichten.AnyAsync(b =>
            b.VanGebruikerId == mijnId && b.NaarGebruikerId == naarGebruikerId && !b.Gelezen && b.Id != bericht.Id);

        if (!heeftAlOngelezen && !string.IsNullOrEmpty(ontvanger.Email))
        {
            var afzender = await _userManager.FindByIdAsync(mijnId!);
            var preview = verzoek.Tekst.Length > 100 ? verzoek.Tekst[..100] + "…" : verzoek.Tekst;
            var url = $"http://localhost:5500/berichten.html?partner={mijnId}&naam={Uri.EscapeDataString(afzender?.Naam ?? "")}&rol={afzender?.Rol ?? ""}";
            _ = _email.VerstuurAsync(ontvanger.Email, ontvanger.Naam,
                $"Nieuw bericht van {afzender?.Naam ?? "iemand"} – RaceCoachFinder",
                EmailTemplates.NieuwBericht(afzender?.Naam ?? "Iemand", preview, url));
        }

        return Ok(bericht);
    }

    // Haal alle berichten op in een gesprek met een specifieke gebruiker
    [HttpGet("gesprek/{partnerId}")]
    public async Task<IActionResult> GetGesprek(string partnerId)
    {
        var mijnId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var berichten = await _context.Berichten
            .Where(b =>
                (b.VanGebruikerId == mijnId && b.NaarGebruikerId == partnerId) ||
                (b.VanGebruikerId == partnerId && b.NaarGebruikerId == mijnId))
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
            .Where(b => b.VanGebruikerId == mijnId || b.NaarGebruikerId == mijnId)
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

    // Aantal ongelezen berichten (voor nav badge)
    [HttpGet("ongelezen")]
    public async Task<IActionResult> GetAantalOngelezen()
    {
        var mijnId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var aantal = await _context.Berichten
            .CountAsync(b => b.NaarGebruikerId == mijnId && !b.Gelezen);
        return Ok(aantal);
    }
}

public record BerichtVerzoek(string Tekst);
