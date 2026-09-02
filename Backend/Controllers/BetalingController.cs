using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Backend.Models;
using Mollie.Api.Client;
using Mollie.Api.Models;
using Mollie.Api.Models.Customer;
using Mollie.Api.Models.Payment;
using Mollie.Api.Models.Payment.Request;
using Mollie.Api.Models.Subscription;
using System.Security.Claims;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BetalingController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _config;
    private readonly AppDbContext _context;

    public BetalingController(UserManager<ApplicationUser> userManager, IConfiguration config, AppDbContext context)
    {
        _userManager = userManager;
        _config = config;
        _context = context;
    }

    private string ApiKey =>
        Environment.GetEnvironmentVariable("MOLLIE_API_KEY")
        ?? _config["Mollie:ApiKey"]
        ?? throw new InvalidOperationException("Mollie API key niet geconfigureerd.");

    private const string BackendUrl = "https://racecoachfinder-production.up.railway.app";
    private const string FrontendUrl = "https://race-coach-finder.vercel.app";

    [HttpPost("start-abonnement")]
    [Authorize(Roles = "Coach")]
    public async Task<IActionResult> StartAbonnement()
    {
        try
        {
            var gebruiker = await _userManager.GetUserAsync(User);
            if (gebruiker == null) return Unauthorized();

            var klantClient = new CustomerClient(ApiKey);
            var betalingClient = new PaymentClient(ApiKey);

            if (string.IsNullOrEmpty(gebruiker.MollieKlantId))
            {
                var email = gebruiker.Email;
                var geldigEmail = !string.IsNullOrEmpty(email) && email.Contains('@') && email.IndexOf('.', email.IndexOf('@')) > 0;
                var klant = await klantClient.CreateCustomerAsync(new CustomerRequest
                {
                    Name = gebruiker.Naam,
                    Email = geldigEmail ? email : null
                });
                gebruiker.MollieKlantId = klant.Id;
                await _userManager.UpdateAsync(gebruiker);
            }

            var betaling = await betalingClient.CreatePaymentAsync(new PaymentRequest
            {
                Amount = new Amount(Currency.EUR, "10.00"),
                Description = "RaceCoachFinder – maandelijks abonnement",
                RedirectUrl = $"{FrontendUrl}/betaling-succes.html",
                WebhookUrl = $"{BackendUrl}/api/betaling/webhook",
                SequenceType = SequenceType.First,
                CustomerId = gebruiker.MollieKlantId
            });

            return Ok(new { checkoutUrl = betaling.Links.Checkout?.Href });
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("StartAbonnement fout: " + ex.ToString());
            return StatusCode(500, ex.Message);
        }
    }

    [HttpGet("status")]
    [Authorize(Roles = "Coach")]
    public async Task<IActionResult> GetStatus()
    {
        var gebruiker = await _userManager.GetUserAsync(User);
        if (gebruiker == null) return Unauthorized();

        var nu = DateTime.UtcNow;
        return Ok(new
        {
            abonnementActief = gebruiker.AbonnementActief && gebruiker.AbonnementVerlooptOp > nu,
            inGratisperiode = gebruiker.GratisVerlooptOp.HasValue && gebruiker.GratisVerlooptOp > nu,
            gratisVerlooptOp = gebruiker.GratisVerlooptOp,
            abonnementVerlooptOp = gebruiker.AbonnementVerlooptOp
        });
    }

    [HttpPost("test-activeer")]
    [Authorize(Roles = "Coach")]
    public async Task<IActionResult> TestActiveer()
    {
        var gebruiker = await _userManager.GetUserAsync(User);
        if (gebruiker == null) return Unauthorized();
        gebruiker.AbonnementActief = true;
        gebruiker.AbonnementVerlooptOp = DateTime.UtcNow.AddMonths(1);
        await _userManager.UpdateAsync(gebruiker);
        return Ok(new { bericht = "Testabonnement geactiveerd." });
    }

    [HttpPost("start-factuur/{boekingId:int}")]
    [Authorize(Roles = "Rijder")]
    public async Task<IActionResult> StartFactuur(int boekingId)
    {
        try
        {
            var rijderId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var boeking = await _context.Boekingen.FindAsync(boekingId);
            if (boeking == null) return NotFound("Boeking niet gevonden.");
            if (boeking.RijderGebruikerId != rijderId) return Forbid();
            if (boeking.Status != "Openstaand") return BadRequest("Boeking is niet meer openstaand.");

            var fee = Math.Round(boeking.Bedrag * 0.02m, 2);
            var totaal = boeking.Bedrag + fee;
            var factuurnummer = boeking.FactuurnummerTekst ?? $"F-{DateTime.UtcNow:yyyy}-{boeking.Id:D4}";

            var betalingClient = new PaymentClient(ApiKey);
            var betaling = await betalingClient.CreatePaymentAsync(new PaymentRequest
            {
                Amount = new Amount(Currency.EUR, totaal.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)),
                Description = $"Factuur {factuurnummer} – {boeking.Omschrijving}",
                RedirectUrl = $"{FrontendUrl}/betaling-succes.html?boekingId={boekingId}",
                WebhookUrl = $"{BackendUrl}/api/betaling/webhook-factuur",
            });

            boeking.MollieBetalingId = betaling.Id;
            await _context.SaveChangesAsync();

            return Ok(new { checkoutUrl = betaling.Links.Checkout?.Href });
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("StartFactuur fout: " + ex.ToString());
            return StatusCode(500, ex.Message);
        }
    }

    [HttpPost("webhook-factuur")]
    [AllowAnonymous]
    public async Task<IActionResult> WebhookFactuur([FromForm] string id)
    {
        try
        {
            var betalingClient = new PaymentClient(ApiKey);
            var betaling = await betalingClient.GetPaymentAsync(id);
            if (betaling.Status?.ToString().ToLower() != "paid") return Ok();

            var boeking = await _context.Boekingen.FirstOrDefaultAsync(b => b.MollieBetalingId == id);
            if (boeking == null) return Ok();

            boeking.Status = "Betaald";
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("WebhookFactuur fout: " + ex.Message);
        }
        return Ok();
    }

    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> Webhook([FromForm] string id)
    {
        try
        {
            var betalingClient = new PaymentClient(ApiKey);
            var betaling = await betalingClient.GetPaymentAsync(id);

            if (betaling.Status?.ToString().ToLower() != "paid") return Ok();

            var gebruiker = await _userManager.Users
                .FirstOrDefaultAsync(u => u.MollieKlantId == betaling.CustomerId);
            if (gebruiker == null) return Ok();

            if (betaling.SequenceType == SequenceType.First)
            {
                var abonnementClient = new SubscriptionClient(ApiKey);
                await abonnementClient.CreateSubscriptionAsync(gebruiker.MollieKlantId!, new SubscriptionRequest
                {
                    Amount = new Amount(Currency.EUR, "10.00"),
                    Interval = "1 month",
                    Description = "RaceCoachFinder maandelijks abonnement",
                    WebhookUrl = $"{BackendUrl}/api/betaling/webhook"
                });
            }

            gebruiker.AbonnementActief = true;
            gebruiker.AbonnementVerlooptOp = DateTime.UtcNow.AddMonths(1).AddDays(3);
            await _userManager.UpdateAsync(gebruiker);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Webhook fout: " + ex.Message);
        }

        return Ok();
    }
}
