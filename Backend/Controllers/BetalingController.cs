using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Models;
using Mollie.Api.Client;
using Mollie.Api.Models;
using Mollie.Api.Models.Customer;
using Mollie.Api.Models.Payment;
using Mollie.Api.Models.Payment.Request;
using Mollie.Api.Models.Subscription;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BetalingController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _config;

    public BetalingController(UserManager<ApplicationUser> userManager, IConfiguration config)
    {
        _userManager = userManager;
        _config = config;
    }

    private string ApiKey =>
        Environment.GetEnvironmentVariable("MOLLIE_API_KEY")
        ?? _config["Mollie:ApiKey"]
        ?? throw new InvalidOperationException("Mollie API key niet geconfigureerd.");

    private const string BackendUrl = "https://racecoachfinder-production.up.railway.app";
    private const string FrontendUrl = "https://racecoachfinder.netlify.app";

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
                var klant = await klantClient.CreateCustomerAsync(new CustomerRequest
                {
                    Name = gebruiker.Naam,
                    Email = gebruiker.Email ?? gebruiker.Naam
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
