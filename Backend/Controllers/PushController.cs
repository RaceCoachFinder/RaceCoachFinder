using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Backend.Models;
using Backend.Services;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PushController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly PushService _push;

    public PushController(AppDbContext context, PushService push)
    {
        _context = context;
        _push = push;
    }

    // Openbare sleutel die de browser nodig heeft om zich aan te melden
    [HttpGet("publieke-sleutel")]
    [AllowAnonymous]
    public async Task<IActionResult> PubliekeSleutel()
    {
        var vapid = await _push.GetVapidAsync();
        return Ok(new { sleutel = vapid.PublicKey });
    }

    [HttpPost("abonneer")]
    [Authorize]
    public async Task<IActionResult> Abonneer([FromBody] PushAbonneerVerzoek verzoek)
    {
        var mijnId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        if (string.IsNullOrWhiteSpace(verzoek.Endpoint) || !verzoek.Endpoint.StartsWith("https://")
            || string.IsNullOrWhiteSpace(verzoek.Keys?.P256dh) || string.IsNullOrWhiteSpace(verzoek.Keys?.Auth))
            return BadRequest("Ongeldige aanmelding.");

        // Hetzelfde apparaat kan maar bij één account horen: bestaande aanmelding overnemen
        var bestaand = await _context.PushAbonnementen.FirstOrDefaultAsync(a => a.Endpoint == verzoek.Endpoint);
        if (bestaand != null)
        {
            bestaand.GebruikerId = mijnId;
            bestaand.P256dh = verzoek.Keys!.P256dh!;
            bestaand.Auth = verzoek.Keys!.Auth!;
        }
        else
        {
            _context.PushAbonnementen.Add(new PushAbonnement
            {
                GebruikerId = mijnId,
                Endpoint = verzoek.Endpoint,
                P256dh = verzoek.Keys!.P256dh!,
                Auth = verzoek.Keys!.Auth!,
                AangemaaktOp = DateTime.UtcNow
            });
        }
        await _context.SaveChangesAsync();
        return Ok();
    }

    [HttpPost("afmelden")]
    [Authorize]
    public async Task<IActionResult> Afmelden([FromBody] PushAfmeldenVerzoek verzoek)
    {
        var mijnId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var a = await _context.PushAbonnementen.FirstOrDefaultAsync(x => x.Endpoint == verzoek.Endpoint && x.GebruikerId == mijnId);
        if (a != null)
        {
            _context.PushAbonnementen.Remove(a);
            await _context.SaveChangesAsync();
        }
        return NoContent();
    }

    [HttpPost("test")]
    [Authorize]
    public async Task<IActionResult> Test()
    {
        var mijnId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var aantal = await _push.StuurAsync(new[] { mijnId }, new PushBericht(
            "RaceCoachFinder", "Meldingen staan aan! Zo ziet een melding eruit.", "/berichten.html", "test"));
        if (aantal == 0) return BadRequest("Er is geen apparaat aangemeld voor meldingen.");
        return Ok(new { apparaten = aantal });
    }
}

public record PushSleutels(string? P256dh, string? Auth);
public record PushAbonneerVerzoek(string Endpoint, PushSleutels? Keys);
public record PushAfmeldenVerzoek(string Endpoint);
