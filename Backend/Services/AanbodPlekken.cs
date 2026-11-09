using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Backend.Models;

namespace Backend.Services;

// Hoeveel rijders doen er mee aan een training (groepschat-leden min de coaches), en is hij vol?
public static class AanbodPlekken
{
    public static async Task<Dictionary<int, int>> AantalRijdersAsync(AppDbContext context, IEnumerable<CoachAanbod> aanboden)
    {
        var lijst = aanboden.ToList();
        var ids = lijst.Select(a => a.Id).ToList();
        var groepen = await context.Groepsgesprekken.Where(g => ids.Contains(g.CoachAanbodId))
            .Select(g => new { g.Id, g.CoachAanbodId }).ToListAsync();
        var groepIds = groepen.Select(g => g.Id).ToList();
        var leden = await context.GroepsgesprekLeden.Where(l => groepIds.Contains(l.GroepsgesprekId))
            .Select(l => new { l.GroepsgesprekId, l.GebruikerId }).ToListAsync();
        var coaches = await context.CoachAanbodUitnodigingen.Where(u => ids.Contains(u.CoachAanbodId) && u.Status != "Afgewezen")
            .Select(u => new { u.CoachAanbodId, u.CoachGebruikerId }).ToListAsync();

        var resultaat = new Dictionary<int, int>();
        foreach (var a in lijst)
        {
            var coachIds = coaches.Where(c => c.CoachAanbodId == a.Id).Select(c => c.CoachGebruikerId).Append(a.CoachGebruikerId).ToHashSet();
            var groep = groepen.Where(g => g.CoachAanbodId == a.Id).OrderBy(g => g.Id).FirstOrDefault();
            resultaat[a.Id] = groep == null ? 0 : leden.Count(l => l.GroepsgesprekId == groep.Id && !coachIds.Contains(l.GebruikerId));
        }
        return resultaat;
    }

    public static async Task<int> AantalRijdersAsync(AppDbContext context, CoachAanbod aanbod) =>
        (await AantalRijdersAsync(context, new[] { aanbod }))[aanbod.Id];

    public static async Task<bool> IsCoachVanAsync(AppDbContext context, CoachAanbod aanbod, string gebruikerId) =>
        aanbod.CoachGebruikerId == gebruikerId ||
        await context.CoachAanbodUitnodigingen.AnyAsync(u => u.CoachAanbodId == aanbod.Id && u.CoachGebruikerId == gebruikerId && u.Status != "Afgewezen");
}

// Plek vrijgekomen bij een volle training: iedereen op de wachtlijst krijgt een pushmelding en een mail
public class WachtlijstMelder
{
    private readonly AppDbContext _context;
    private readonly PushService _push;
    private readonly IEmailService _email;
    private readonly ILogger<WachtlijstMelder> _logger;

    public WachtlijstMelder(AppDbContext context, PushService push, IEmailService email, ILogger<WachtlijstMelder> logger)
    {
        _context = context;
        _push = push;
        _email = email;
        _logger = logger;
    }

    public async Task MeldAlsPlekVrijAsync(int aanbodId)
    {
        try
        {
            var aanbod = await _context.CoachAanboden.FindAsync(aanbodId);
            if (aanbod == null || !aanbod.IsActief || aanbod.MaxRijders == null || aanbod.Datum.Date < DateTime.UtcNow.Date) return;
            if (await AanbodPlekken.AantalRijdersAsync(_context, aanbod) >= aanbod.MaxRijders) return;

            var wachtenden = await _context.AanbodWachtlijsten.Where(w => w.CoachAanbodId == aanbodId).Select(w => w.GebruikerId).ToListAsync();
            if (wachtenden.Count == 0) return;
            var gebruikers = await _context.Users.Where(u => wachtenden.Contains(u.Id)).ToListAsync();
            var url = "https://www.racecoachfinder.nl/trainingen.html";
            foreach (var g in gebruikers)
            {
                var taal = await EmailTaal.VoorAsync(_context, g.Id);
                try
                {
                    await _push.StuurAsync(new[] { g.Id }, new PushBericht(
                        taal == "en" ? "🏁 A spot is available!" : "🏁 Er is een plek vrij!",
                        aanbod.Titel, "/trainingen.html", $"plek-{aanbod.Id}"));
                }
                catch (Exception ex) { _logger.LogWarning("Push plek vrij mislukt: {Fout}", ex.Message); }

                if (!string.IsNullOrEmpty(g.Email) && g.Email.Contains('@'))
                {
                    try
                    {
                        await _email.VerstuurAsync(g.Email, g.Naam, EmailTemplates.PlekVrijOnderwerp(aanbod.Titel, taal),
                            EmailTemplates.PlekVrij(g.Naam, aanbod.Titel, aanbod.Datum, aanbod.Locatie, url, taal));
                    }
                    catch (Exception ex) { _logger.LogWarning("Mail plek vrij mislukt: {Fout}", ex.Message); }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Wachtlijst melden mislukt: {Fout}", ex.Message);
        }
    }
}
