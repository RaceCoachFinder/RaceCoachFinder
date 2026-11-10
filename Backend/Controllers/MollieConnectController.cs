using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Services;

namespace Backend.Controllers;

// Coach koppelt zijn eigen Mollie-account, zodat rijders hem rechtstreeks betalen
[ApiController]
[Route("api/[controller]")]
public class MollieConnectController : ControllerBase
{
    private readonly MollieConnectService _connect;
    private readonly ILogger<MollieConnectController> _logger;
    private readonly Backend.Data.AppDbContext _context;

    public MollieConnectController(MollieConnectService connect, ILogger<MollieConnectController> logger, Backend.Data.AppDbContext context)
    {
        _connect = connect;
        _logger = logger;
        _context = context;
    }

    private static string FrontendUrl =>
        Environment.GetEnvironmentVariable("FRONTEND_URL") is { Length: > 0 } f ? f.TrimEnd('/') : "https://www.racecoachfinder.nl";

    private string MijnId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet("status")]
    [Authorize(Roles = "Coach")]
    public async Task<IActionResult> Status([FromQuery] bool vernieuw = false)
    {
        var status = await _connect.StatusVoorAsync(MijnId, vernieuw);
        var k = await _connect.KoppelingAsync(MijnId);
        var (keuze, geenBetaalWijze) = await _connect.BetaalKeuzeAsync(MijnId);
        var coach = await _context.Coaches.AsNoTracking().FirstOrDefaultAsync(c => c.GebruikerId == MijnId);
        return Ok(new
        {
            ingesteld = MollieConnectService.IsIngesteld,
            gekoppeld = k != null,
            klaar = status == KoppelStatus.Klaar,
            status = status.ToString(),
            organisatie = k?.OrganisatieNaam,
            onboardingStatus = k?.OnboardingStatus,
            gekoppeldOp = k?.GekoppeldOp,
            testmodus = MollieConnectService.Testmodus,
            melding = status == KoppelStatus.Klaar ? null : MollieConnectService.MeldingVoorCoach(status),
            // Hoe rijders deze coach nu betalen: "online", "overmaken" of null (nog niets ingesteld)
            betaalWijze = keuze?.Wijze,
            betaalMelding = geenBetaalWijze,
            iban = IbanHelper.Opmaak(coach?.Iban),
            ibanTenaamstelling = coach?.IbanTenaamstelling
        });
    }

    public record StartVerzoek(bool NieuwAccount = false, string? Taal = null);

    [HttpPost("start")]
    [Authorize(Roles = "Coach")]
    public IActionResult Start([FromBody] StartVerzoek? verzoek)
    {
        if (!MollieConnectService.IsIngesteld)
            return StatusCode(503, MollieConnectService.MeldingVoorCoach(KoppelStatus.NietIngesteld));
        var url = _connect.MaakAutorisatieUrl(MijnId, verzoek?.NieuwAccount ?? false, verzoek?.Taal == "en" ? "en" : "nl");
        return Ok(new { url });
    }

    // Mollie stuurt de coach hierheen terug na het koppelen
    [HttpGet("terug")]
    [AllowAnonymous]
    public async Task<IActionResult> Terug([FromQuery] string? code, [FromQuery] string? state, [FromQuery] string? error)
    {
        string Naar(string resultaat, string? reden = null) =>
            $"{FrontendUrl}/instellingen.html?mollie={resultaat}" +
            (reden != null ? "&reden=" + Uri.EscapeDataString(reden) : "") + "#mollie";

        var gebruikerId = _connect.GebruikerUitState(state);
        if (gebruikerId == null)
            return Redirect(Naar("fout", "De koppeling is verlopen. Probeer het opnieuw."));
        if (!string.IsNullOrEmpty(error) || string.IsNullOrEmpty(code))
            return Redirect(Naar("geannuleerd"));

        try
        {
            var fout = await _connect.VerwerkTerugkeerAsync(gebruikerId, code);
            return Redirect(fout == null ? Naar("gekoppeld") : Naar("fout", fout));
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Mollie koppelen mislukt: {Fout}", ex.Message);
            return Redirect(Naar("fout", "Koppelen met Mollie is niet gelukt. Probeer het later opnieuw."));
        }
    }

    [HttpPost("ontkoppel")]
    [Authorize(Roles = "Coach")]
    public async Task<IActionResult> Ontkoppel()
    {
        await _connect.OntkoppelAsync(MijnId);
        return NoContent();
    }
}
