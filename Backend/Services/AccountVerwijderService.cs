using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Mollie.Api.Client;
using Mollie.Api.Models.Subscription;
using Backend.Data;
using Backend.Models;

namespace Backend.Services;

// Verwijdert een account met alles erbij: profiel, berichten, groepsberichten, boekingen, reviews,
// favorieten, aanvragen, aanbiedingen, agenda, meldingen, instellingen en profielfoto.
// Een lopend abonnement bij Mollie wordt eerst opgezegd, zodat er daarna niets meer wordt afgeschreven.
public class AccountVerwijderService
{
    private readonly AppDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _config;
    private readonly ILogger<AccountVerwijderService> _logger;

    public AccountVerwijderService(AppDbContext context, UserManager<ApplicationUser> userManager, IConfiguration config, ILogger<AccountVerwijderService> logger)
    {
        _context = context;
        _userManager = userManager;
        _config = config;
        _logger = logger;
    }

    // null = gelukt, anders een foutmelding voor de gebruiker
    public async Task<string?> VerwijderAsync(string gebruikerId)
    {
        var gebruiker = await _userManager.FindByIdAsync(gebruikerId);
        if (gebruiker == null) return "Account niet gevonden.";
        var id = gebruiker.Id;

        // 1. Abonnement opzeggen (anders blijft Mollie afschrijven)
        if (!string.IsNullOrEmpty(gebruiker.MollieKlantId))
        {
            var sleutel = Environment.GetEnvironmentVariable("MOLLIE_API_KEY") ?? _config["Mollie:ApiKey"];
            try
            {
                var client = new SubscriptionClient(sleutel!);
                var lijst = await client.GetSubscriptionListAsync(gebruiker.MollieKlantId, null, 50);
                foreach (var a in lijst.Items.Where(a => a.Status == SubscriptionStatus.Active || a.Status == SubscriptionStatus.Pending))
                    await client.CancelSubscriptionAsync(gebruiker.MollieKlantId, a.Id);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Abonnement opzeggen bij account verwijderen mislukt voor {Id}: {Fout}", id, ex.Message);
                return "Je abonnement kon niet worden opgezegd, daarom is je account (nog) niet verwijderd. Probeer het later opnieuw of neem contact met ons op.";
            }
        }

        // 2. Profielen en favorieten
        var coachIds = await _context.Coaches.Where(c => c.GebruikerId == id).Select(c => c.Id).ToListAsync();
        _context.CoachFavorieten.RemoveRange(_context.CoachFavorieten.Where(f => f.RijderId == id || coachIds.Contains(f.CoachId)));
        _context.RijderFavorieten.RemoveRange(_context.RijderFavorieten.Where(f => f.CoachId == id || f.RijderId == id));
        _context.Coaches.RemoveRange(_context.Coaches.Where(c => c.GebruikerId == id));
        _context.Rijders.RemoveRange(_context.Rijders.Where(r => r.GebruikerId == id));

        // 3. Berichten, boekingen, reviews, agenda, meldingen, instellingen
        _context.Berichten.RemoveRange(_context.Berichten.Where(b => b.VanGebruikerId == id || b.NaarGebruikerId == id));
        _context.Boekingen.RemoveRange(_context.Boekingen.Where(b => b.CoachGebruikerId == id || b.RijderGebruikerId == id));
        _context.Reviews.RemoveRange(_context.Reviews.Where(r => r.CoachGebruikerId == id || r.RijderGebruikerId == id));
        _context.AgendaItems.RemoveRange(_context.AgendaItems.Where(a => a.CoachGebruikerId == id));
        _context.PushAbonnementen.RemoveRange(_context.PushAbonnementen.Where(p => p.GebruikerId == id));
        _context.GebruikerInstellingen.RemoveRange(_context.GebruikerInstellingen.Where(i => i.GebruikerId == id));
        _context.AanbodWachtlijsten.RemoveRange(_context.AanbodWachtlijsten.Where(w => w.GebruikerId == id));

        // 4. Aanvragen, aanbiedingen en uitnodigingen
        _context.RijderAanvragen.RemoveRange(_context.RijderAanvragen.Where(a => a.RijderId == id));
        var aanbodIds = await _context.CoachAanboden.Where(a => a.CoachGebruikerId == id).Select(a => a.Id).ToListAsync();
        _context.CoachAanbodUitnodigingen.RemoveRange(_context.CoachAanbodUitnodigingen
            .Where(u => u.CoachGebruikerId == id || u.RijderGebruikerId == id || aanbodIds.Contains(u.CoachAanbodId)));
        _context.AanbodWachtlijsten.RemoveRange(_context.AanbodWachtlijsten.Where(w => aanbodIds.Contains(w.CoachAanbodId)));
        _context.CoachAanboden.RemoveRange(_context.CoachAanboden.Where(a => a.CoachGebruikerId == id));
        _context.TrainingVoorstellen.RemoveRange(_context.TrainingVoorstellen.Where(v => v.OrganisatorId == id));

        // 5. Groepen: berichten van deze persoon weg, lidmaatschap weg; lege groepen worden opgeruimd
        var groepIds = await _context.GroepsgesprekLeden.Where(l => l.GebruikerId == id).Select(l => l.GroepsgesprekId).ToListAsync();
        _context.Groepsberichten.RemoveRange(_context.Groepsberichten.Where(b => b.VanGebruikerId == id));
        _context.GroepsgesprekLeden.RemoveRange(_context.GroepsgesprekLeden.Where(l => l.GebruikerId == id));
        _context.GroepsgesprekVerwijderingen.RemoveRange(_context.GroepsgesprekVerwijderingen.Where(v => v.GebruikerId == id));

        await _context.SaveChangesAsync();
        await GroepOpruimer.VerwijderLegeGroepenAsync(_context, groepIds);

        // 6. Profielfoto
        try
        {
            var map = Environment.GetEnvironmentVariable("UPLOADS_PATH") ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            var foto = Path.Combine(map, $"foto-{id}.jpg");
            if (File.Exists(foto)) File.Delete(foto);
        }
        catch (Exception ex) { _logger.LogWarning("Profielfoto verwijderen mislukt: {Fout}", ex.Message); }

        // 7. Het account zelf
        var resultaat = await _userManager.DeleteAsync(gebruiker);
        if (!resultaat.Succeeded)
            return "Account verwijderen mislukt: " + string.Join(", ", resultaat.Errors.Select(e => e.Description));
        _logger.LogInformation("Account {Id} en alle bijbehorende gegevens verwijderd.", id);
        return null;
    }
}
