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
    private readonly IEmailVerificatieService _verificatie;
    private readonly ILogger<AuthController> _logger;
    private readonly Backend.Data.AppDbContext _context;

    public AuthController(UserManager<ApplicationUser> userManager, IConfiguration config, IEmailService email, IWachtwoordResetService resetService, IEmailVerificatieService verificatie, ILogger<AuthController> logger, Backend.Data.AppDbContext context)
    {
        _context = context;
        _userManager = userManager;
        _config = config;
        _email = email;
        _resetService = resetService;
        _verificatie = verificatie;
        _logger = logger;
    }

    private void StuurEmailAsync(string email, string naam, string onderwerp, string html, [System.Runtime.CompilerServices.CallerMemberName] string aanroeper = "")
    {
        _ = _email.VerstuurAsync(email, naam, onderwerp, html)
            .ContinueWith(t => _logger.LogError(t.Exception, "E-mail mislukt [{Aanroeper}] naar {Email}: {Onderwerp}", aanroeper, email, onderwerp),
                TaskContinuationOptions.OnlyOnFaulted);
    }

    [HttpPost("registreren")]
    [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("mail")]
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
            if (await _userManager.IsLockedOutAsync(bestaand))
                return StatusCode(429, "Te veel mislukte pogingen. Probeer het over 15 minuten opnieuw.");
            if (!await _userManager.CheckPasswordAsync(bestaand, verzoek.Wachtwoord))
            {
                await _userManager.AccessFailedAsync(bestaand);
                return BadRequest("Onjuist wachtwoord voor het bestaande account.");
            }
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
            EmailConfirmed = false,
            MarketingToestemming = verzoek.MarketingToestemming,
            AangemaaktOp = DateTime.UtcNow
        };

        var resultaat = await _userManager.CreateAsync(gebruiker, verzoek.Wachtwoord);
        if (!resultaat.Succeeded)
        {
            var fouten = resultaat.Errors.Select(e => e.Description);
            return BadRequest(string.Join(", ", fouten));
        }

        // Taal van de e-mails = de taal waarin de site stond bij het registreren (later te wijzigen in Instellingen)
        var taal = verzoek.Taal == "en" ? "en" : "nl";
        if (taal == "en")
        {
            _context.GebruikerInstellingen.Add(new GebruikerInstellingen { GebruikerId = gebruiker.Id, EmailTaal = "en" });
            await _context.SaveChangesAsync();
        }

        var code = _verificatie.MaakCode(verzoek.Email);
        StuurEmailAsync(gebruiker.Email!, gebruiker.Naam,
            EmailTemplates.EmailBevestigingOnderwerp(taal),
            EmailTemplates.EmailBevestiging(gebruiker.Naam, code, taal));

        return Ok(new { bericht = "Account aangemaakt. Controleer je e-mail voor de bevestigingscode.", wachtOpBevestiging = true, email = verzoek.Email });
    }

    [HttpPost("inloggen")]
    public async Task<IActionResult> Inloggen(InloggenVerzoek verzoek)
    {
        // Probeer eerst op e-mail, daarna op gebruikersnaam (voor code-accounts)
        var gebruiker = await _userManager.FindByEmailAsync(verzoek.Email)
                     ?? await _userManager.FindByNameAsync(verzoek.Email);
        if (gebruiker == null) return Unauthorized("Onjuist e-mailadres of wachtwoord.");

        // Na 5 foute pogingen 15 minuten geblokkeerd (tegen het raden van wachtwoorden)
        if (await _userManager.IsLockedOutAsync(gebruiker))
            return StatusCode(429, "Te veel mislukte pogingen. Probeer het over 15 minuten opnieuw of stel een nieuw wachtwoord in.");
        if (!await _userManager.CheckPasswordAsync(gebruiker, verzoek.Wachtwoord))
        {
            await _userManager.AccessFailedAsync(gebruiker);
            return Unauthorized("Onjuist e-mailadres of wachtwoord.");
        }
        if (gebruiker.AccessFailedCount > 0) await _userManager.ResetAccessFailedCountAsync(gebruiker);

        // Het oude standaardwachtwoord van het admin-account staat in de openbare code: niet meer toestaan
        if (gebruiker.Rol.Split(',').Any(r => r.Trim() == "Admin") && verzoek.Wachtwoord == "Admin")
            return Unauthorized("Het standaardwachtwoord van het admin-account is uitgeschakeld. Stel een eigen wachtwoord in via ADMIN_WACHTWOORD bij Railway.");

        if (!gebruiker.EmailConfirmed)
        {
            var code = _verificatie.MaakCode(verzoek.Email);
            var taal = await EmailTaal.VoorAsync(_context, gebruiker.Id);
            StuurEmailAsync(gebruiker.Email!, gebruiker.Naam,
                EmailTemplates.EmailBevestigingOnderwerp(taal),
                EmailTemplates.EmailBevestiging(gebruiker.Naam, code, taal));
            return Unauthorized(new { fout = "email_niet_bevestigd", email = verzoek.Email });
        }

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
    [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("mail")]
    public async Task<IActionResult> WachtwoordVergeten([FromBody] WachtwoordVergetenVerzoek verzoek)
    {
        var gebruiker = await _userManager.FindByEmailAsync(verzoek.Email);
        if (gebruiker != null)
        {
            var code = _resetService.MaakCode(verzoek.Email);
            var taal = await EmailTaal.VoorAsync(_context, gebruiker.Id);
            StuurEmailAsync(gebruiker.Email ?? verzoek.Email, gebruiker.Naam,
                EmailTemplates.WachtwoordResetOnderwerp(taal),
                EmailTemplates.WachtwoordReset(gebruiker.Naam, code, taal));
        }
        return Ok();
    }

    [HttpPost("wachtwoord-reset")]
    public async Task<IActionResult> WachtwoordReset([FromBody] WachtwoordResetVerzoek verzoek)
    {
        if (!_resetService.ValideerCode(verzoek.Email, verzoek.Code))
            return BadRequest("Code is ongeldig of verlopen. Na 5 foute pogingen moet je een nieuwe code aanvragen.");

        var gebruiker = await _userManager.FindByEmailAsync(verzoek.Email);
        if (gebruiker == null) return BadRequest("Gebruiker niet gevonden.");

        var token = await _userManager.GeneratePasswordResetTokenAsync(gebruiker);
        var resultaat = await _userManager.ResetPasswordAsync(gebruiker, token, verzoek.NieuwWachtwoord);

        if (!resultaat.Succeeded)
            return BadRequest(string.Join(", ", resultaat.Errors.Select(e => e.Description)));

        _resetService.VerwijderCode(verzoek.Email);
        // Nieuw wachtwoord = blokkade door foute inlogpogingen opheffen
        await _userManager.SetLockoutEndDateAsync(gebruiker, null);
        await _userManager.ResetAccessFailedCountAsync(gebruiker);
        return Ok();
    }

    [HttpPost("bevestig-email")]
    public async Task<IActionResult> BevestigEmail([FromBody] EmailBevestigenVerzoek verzoek)
    {
        if (!_verificatie.ValideerCode(verzoek.Email, verzoek.Code))
            return BadRequest("Code is ongeldig of verlopen. Na 5 foute pogingen moet je een nieuwe code aanvragen.");

        var gebruiker = await _userManager.FindByEmailAsync(verzoek.Email);
        if (gebruiker == null) return BadRequest("Gebruiker niet gevonden.");

        gebruiker.EmailConfirmed = true;
        await _userManager.UpdateAsync(gebruiker);
        _verificatie.VerwijderCode(verzoek.Email);

        var welkomTaal = await EmailTaal.VoorAsync(_context, gebruiker.Id);
        StuurEmailAsync(gebruiker.Email!, gebruiker.Naam,
            EmailTemplates.WelkomOnderwerp(welkomTaal),
            EmailTemplates.Welkom(gebruiker.Naam, gebruiker.Rol, welkomTaal));

        var token = MaakToken(gebruiker);
        return Ok(new InloggenAntwoord(token, gebruiker.Id, gebruiker.Naam, gebruiker.Email!, gebruiker.Rol, gebruiker.HeeftAccountIngericht));
    }

    [HttpPost("verificatie-opnieuw")]
    [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("mail")]
    public async Task<IActionResult> VerificatieOpnieuw([FromBody] WachtwoordVergetenVerzoek verzoek)
    {
        var gebruiker = await _userManager.FindByEmailAsync(verzoek.Email);
        if (gebruiker != null && !gebruiker.EmailConfirmed)
        {
            var code = _verificatie.MaakCode(verzoek.Email);
            var taal = await EmailTaal.VoorAsync(_context, gebruiker.Id);
            StuurEmailAsync(gebruiker.Email!, gebruiker.Naam,
                EmailTemplates.EmailBevestigingOnderwerp(taal),
                EmailTemplates.EmailBevestiging(gebruiker.Naam, code, taal));
        }
        return Ok();
    }

    // Ingelogd: wachtwoord wijzigen met het huidige wachtwoord
    [HttpPost("wachtwoord-wijzigen")]
    [Authorize]
    public async Task<IActionResult> WachtwoordWijzigen([FromBody] WachtwoordWijzigenVerzoek verzoek)
    {
        var gebruiker = await _userManager.FindByIdAsync(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        if (gebruiker == null) return Unauthorized();
        if (string.IsNullOrEmpty(verzoek.Nieuw) || verzoek.Nieuw.Length < 8)
            return BadRequest("Je nieuwe wachtwoord moet minimaal 8 tekens zijn.");
        if (await _userManager.IsLockedOutAsync(gebruiker))
            return StatusCode(429, "Te veel mislukte pogingen. Probeer het over 15 minuten opnieuw.");
        if (!await _userManager.CheckPasswordAsync(gebruiker, verzoek.Huidig ?? ""))
        {
            await _userManager.AccessFailedAsync(gebruiker);
            return BadRequest("Je huidige wachtwoord klopt niet.");
        }
        var resultaat = await _userManager.ChangePasswordAsync(gebruiker, verzoek.Huidig!, verzoek.Nieuw);
        if (!resultaat.Succeeded)
            return BadRequest(string.Join(", ", resultaat.Errors.Select(e => e.Description)));
        return Ok();
    }

    // Ingelogd: eigen account met alles erbij verwijderen (wachtwoord ter bevestiging)
    [HttpPost("account-verwijderen")]
    [Authorize]
    public async Task<IActionResult> AccountVerwijderen([FromBody] AccountVerwijderenVerzoek verzoek, [FromServices] AccountVerwijderService verwijderService)
    {
        var gebruiker = await _userManager.FindByIdAsync(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        if (gebruiker == null) return Unauthorized();
        if ((gebruiker.Rol ?? "").Split(',').Any(r => r.Trim() == "Admin"))
            return BadRequest("Het admin-account kan niet verwijderd worden.");
        if (await _userManager.IsLockedOutAsync(gebruiker))
            return StatusCode(429, "Te veel mislukte pogingen. Probeer het over 15 minuten opnieuw.");
        if (!await _userManager.CheckPasswordAsync(gebruiker, verzoek.Wachtwoord ?? ""))
        {
            await _userManager.AccessFailedAsync(gebruiker);
            return BadRequest("Je wachtwoord klopt niet.");
        }
        var fout = await verwijderService.VerwijderAsync(gebruiker.Id);
        if (fout != null) return BadRequest(fout);
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
public record WachtwoordWijzigenVerzoek(string? Huidig, string Nieuw);
public record AccountVerwijderenVerzoek(string? Wachtwoord);
public record WachtwoordResetVerzoek(string Email, string Code, string NieuwWachtwoord);
public record EmailBevestigenVerzoek(string Email, string Code);
