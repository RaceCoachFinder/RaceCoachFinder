using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Backend.Data;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UploadController : ControllerBase
{
    private readonly AppDbContext _context;

    public UploadController(AppDbContext context)
    {
        _context = context;
    }

    private static string GetUploadsMap() =>
        Environment.GetEnvironmentVariable("UPLOADS_PATH")
        ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");

    [Authorize]
    [HttpPost("profielfoto")]
    public async Task<IActionResult> UploadProfielfoto(IFormFile bestand, [FromQuery] string? profiel)
    {
        if (bestand == null || bestand.Length == 0)
            return BadRequest("Geen bestand ontvangen.");

        if (bestand.Length > 5 * 1024 * 1024)
            return BadRequest("Bestand mag maximaal 5MB zijn.");

        if (!bestand.ContentType.StartsWith("image/"))
            return BadRequest("Alleen afbeeldingen zijn toegestaan.");

        var gebruikerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var bestandsnaam = $"foto-{gebruikerId}.jpg";
        var uploadsMap = GetUploadsMap();
        Directory.CreateDirectory(uploadsMap);
        var pad = Path.Combine(uploadsMap, bestandsnaam);

        using var stroom = new FileStream(pad, FileMode.Create);
        await bestand.CopyToAsync(stroom);

        // ?v= zorgt dat browsers na een nieuwe upload niet de oude (gecachte) foto tonen
        var url = $"/api/upload/foto/{bestandsnaam}?v={DateTime.UtcNow.Ticks}";

        // Direct op het bestaande profiel zetten, zodat de foto niet verloren gaat
        // als iemand daarna niet op 'Profiel opslaan' drukt
        var opgeslagen = false;
        if (profiel == "coach")
        {
            var coach = await _context.Coaches.FirstOrDefaultAsync(c => c.GebruikerId == gebruikerId);
            if (coach != null) { coach.FotoUrl = url; opgeslagen = true; }
        }
        else if (profiel == "rijder")
        {
            var rijder = await _context.Rijders.FirstOrDefaultAsync(r => r.GebruikerId == gebruikerId);
            if (rijder != null) { rijder.FotoUrl = url; opgeslagen = true; }
        }
        if (opgeslagen) await _context.SaveChangesAsync();

        return Ok(new { url, opgeslagen });
    }

    [HttpGet("foto/{bestandsnaam}")]
    public IActionResult GetFoto(string bestandsnaam)
    {
        if (bestandsnaam.Contains("..") || bestandsnaam.Contains('/'))
            return BadRequest();

        var pad = Path.Combine(GetUploadsMap(), bestandsnaam);
        if (!System.IO.File.Exists(pad)) return NotFound();

        return PhysicalFile(pad, "image/jpeg");
    }
}
