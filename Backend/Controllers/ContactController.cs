using Microsoft.AspNetCore.Mvc;
using Backend.Services;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ContactController : ControllerBase
{
    private readonly IEmailService _email;

    public ContactController(IEmailService email)
    {
        _email = email;
    }

    [HttpPost]
    public async Task<IActionResult> VerstuurContactBericht([FromBody] ContactVerzoek verzoek)
    {
        if (string.IsNullOrWhiteSpace(verzoek.Naam) ||
            string.IsNullOrWhiteSpace(verzoek.Email) ||
            string.IsNullOrWhiteSpace(verzoek.Bericht))
            return BadRequest("Vul alle verplichte velden in.");

        var html = "<div style='font-family:sans-serif;max-width:600px'>" +
                   "<h2 style='color:#1d1d2e'>Nieuw contactbericht</h2>" +
                   "<table style='width:100%;border-collapse:collapse;margin-bottom:1rem'>" +
                   "<tr><td style='padding:6px 0;color:#666;width:100px'>Van</td><td style='padding:6px 0;font-weight:600'>" + verzoek.Naam + "</td></tr>" +
                   "<tr><td style='padding:6px 0;color:#666'>E-mail</td><td style='padding:6px 0'><a href='mailto:" + verzoek.Email + "'>" + verzoek.Email + "</a></td></tr>" +
                   "<tr><td style='padding:6px 0;color:#666'>Onderwerp</td><td style='padding:6px 0'>" + (verzoek.Onderwerp ?? "–") + "</td></tr>" +
                   "</table>" +
                   "<hr style='border:none;border-top:1px solid #eee;margin:1rem 0'>" +
                   "<p style='white-space:pre-line;color:#333'>" + verzoek.Bericht + "</p>" +
                   "</div>";

        await _email.VerstuurAsync(
            "racecoachfinder@gmail.com",
            "RaceCoachFinder",
            "Contact: " + (verzoek.Onderwerp ?? verzoek.Naam),
            html
        );

        return Ok();
    }
}

public record ContactVerzoek(string Naam, string Email, string? Onderwerp, string Bericht);
