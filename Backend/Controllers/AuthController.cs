using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Backend.Models;
using Backend.Services;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _config;
    private readonly IEmailService _email;
    private readonly IWachtwoordResetService _resetService;

    public AuthController(UserManager<ApplicationUser> userManager, IConfiguration config, IEmailService email, IWachtwoordResetService resetService)
    {
        _userManager = userManager;
        _config = config;
        _email = email;
        _resetService = resetService;
    }

    [HttpPost("registreren")]
    public async Task<IActionResult> Registreren(RegistrerenVerzoek verzoek)
    {
        if (verzoek.Rol != "Coach" && verzoek.Rol != "Rijder")
            return BadRequest("Ongeldige rol. Kies Coach of Rijder.");

        var bestaand = await _userManager.FindByEmailAsync(verzoek.Email);
        if (bestaand != null)
        {
            var bestaandeRollen = bestaand.Rol.Split(',').Select(r => r.Trim()).ToArray();
            if (bestaandeRollen.Contains(verzoek.Rol))
                return BadRequest("Je hebt al een " + verzoek.Rol + " account met dit e-mailadres.");
            if (!await _userManager.CheckPasswordAsync(bestaand, verzoek.Wachtwoord))
                return BadRequest("Onjuist wachtwoord voor het bestaande account.");
            bestaand.Rol = "Coach,Rijder";
            await _userManager.UpdateAsync(bestaand);
            return Ok(new { bericht = "Tweede rol toegevoegd. Je kan nu inloggen.", tweedeRol = true });
        }

        var gebruiker = new ApplicationUser
        {
            UserName = verzoek.Email,
            Email = verzoek.Email,
            Naam = verzoek.Naam,
            Rol = verzoek.Rol,
            EmailConfirmed = true
        };

        var resultaat = await _userManager.CreateAsync(gebruiker, verzoek.Wachtwoord);
        if (!resultaat.Succeeded)
        {
            var fouten = resultaat.Errors.Select(e => e.Description);
            return BadRequest(string.Join(", ", fouten));
        }

        _ = _email.VerstuurAsync(gebruiker.Email!, gebruiker.Naam,
            "Welkom bij RaceCoachFinder!",
            EmailTemplates.Welkom(gebruiker.Naam, gebruiker.Rol));

        return Ok(new { bericht = "Account aangemaakt. Je kan nu inloggen." });
    }

    [HttpPost("inloggen")]
    public async Task<IActionResult> Inloggen(InloggenVerzoek verzoek)
    {
        // Probeer eerst op e-mail, daarna op gebruikersnaam (voor code-accounts)
        var gebruiker = await _userManager.FindByEmailAsync(verzoek.Email)
                     ?? await _userManager.FindByNameAsync(verzoek.Email);
        if (gebruiker == null || !await _userManager.CheckPasswordAsync(gebruiker, verzoek.Wachtwoord))
            return Unauthorized("Onjuist e-mailadres of wachtwoord.");

        var token = MaakToken(gebruiker);
        return Ok(new InloggenAntwoord(token, gebruiker.Id, gebruiker.Naam, gebruiker.Email!, gebruiker.Rol, gebruiker.HeeftAccountIngericht));
    }

    [HttpPost("richt-account-in")]
    [Authorize(Roles = "Coach")]
    public async Task<IActionResult> RichtAccountIn([FromBody] AccountInrichtenVerzoek verzoek)
    {
        var mijnId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var gebruiker = await _userManager.FindByIdAsync(mijnId!);
        if (gebruiker == null) return NotFound();

        if (!string.IsNullOrWhiteSpace(verzoek.NieuwEmail))
        {
            var bestaand = await _userManager.FindByEmailAsync(verzoek.NieuwEmail);
            if (bestaand != null && bestaand.Id != gebruiker.Id)
                return BadRequest("Dit e-mailadres is al in gebruik.");

            gebruiker.Email = verzoek.NieuwEmail;
            gebruiker.NormalizedEmail = verzoek.NieuwEmail.ToUpperInvariant();
            gebruiker.UserName = verzoek.NieuwEmail;
            gebruiker.NormalizedUserName = verzoek.NieuwEmail.ToUpperInvariant();
        }

        gebruiker.HeeftAccountIngericht = true;
        await _userManager.UpdateAsync(gebruiker);

        var resetToken = await _userManager.GeneratePasswordResetTokenAsync(gebruiker);
        var resultaat = await _userManager.ResetPasswordAsync(gebruiker, resetToken, verzoek.NieuwWachtwoord);
        if (!resultaat.Succeeded)
            return BadRequest(string.Join(", ", resultaat.Errors.Select(e => e.Description)));

        var token = MaakToken(gebruiker);
        return Ok(new InloggenAntwoord(token, gebruiker.Id, gebruiker.Naam, gebruiker.Email!, gebruiker.Rol, true));
    }

    [HttpPost("wachtwoord-vergeten")]
    public async Task<IActionResult> WachtwoordVergeten([FromBody] WachtwoordVergetenVerzoek verzoek)
    {
        var gebruiker = await _userManager.FindByEmailAsync(verzoek.Email);
        if (gebruiker != null)
        {
            var code = _resetService.MaakCode(verzoek.Email);
            _ = _email.VerstuurAsync(verzoek.Email, gebruiker.Naam,
                "Wachtwoord resetten – RaceCoachFinder",
                EmailTemplates.WachtwoordReset(gebruiker.Naam, code));
        }
        return Ok();
    }

    [HttpPost("wachtwoord-reset")]
    public async Task<IActionResult> WachtwoordReset([FromBody] WachtwoordResetVerzoek verzoek)
    {
        if (!_resetService.ValideerCode(verzoek.Email, verzoek.Code))
            return BadRequest("Code is ongeldig of verlopen.");

        var gebruiker = await _userManager.FindByEmailAsync(verzoek.Email);
        if (gebruiker == null) return BadRequest("Gebruiker niet gevonden.");

        var token = await _userManager.GeneratePasswordResetTokenAsync(gebruiker);
        var resultaat = await _userManager.ResetPasswordAsync(gebruiker, token, verzoek.NieuwWachtwoord);

        if (!resultaat.Succeeded)
            return BadRequest(string.Join(", ", resultaat.Errors.Select(e => e.Description)));

        _resetService.VerwijderCode(verzoek.Email);
        return Ok();
    }

    private string MaakToken(ApplicationUser gebruiker)
    {
        var sleutel = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Sleutel"]!));
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, gebruiker.Id),
            new Claim(ClaimTypes.Email, gebruiker.Email!),
            new Claim(ClaimTypes.Name, gebruiker.Naam),
        };
        foreach (var rol in gebruiker.Rol.Split(','))
            claims.Add(new Claim(ClaimTypes.Role, rol.Trim()));

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Uitgever"],
            audience: _config["Jwt:Publiek"],
            claims: claims,
            expires: DateTime.UtcNow.AddDays(int.Parse(_config["Jwt:VerloopDagen"]!)),
            signingCredentials: new SigningCredentials(sleutel, SecurityAlgorithms.HmacSha256)
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

public record WachtwoordVergetenVerzoek(string Email);
public record WachtwoordResetVerzoek(string Email, string Code, string NieuwWachtwoord);
