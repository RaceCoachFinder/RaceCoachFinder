using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UploadController : ControllerBase
{
    private static string GetUploadsMap() =>
        Environment.GetEnvironmentVariable("UPLOADS_PATH")
        ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");

    [Authorize]
    [HttpPost("profielfoto")]
    public async Task<IActionResult> UploadProfielfoto(IFormFile bestand)
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

        return Ok(new { url = $"/api/upload/foto/{bestandsnaam}" });
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
