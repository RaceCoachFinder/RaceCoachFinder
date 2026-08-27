using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UploadController : ControllerBase
{
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
        var uploadsMap = Environment.GetEnvironmentVariable("UPLOADS_PATH")
            ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
        Directory.CreateDirectory(uploadsMap);
        var pad = Path.Combine(uploadsMap, bestandsnaam);

        using var stroom = new FileStream(pad, FileMode.Create);
        await bestand.CopyToAsync(stroom);

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        return Ok(new { url = $"{baseUrl}/uploads/{bestandsnaam}" });
    }
}
