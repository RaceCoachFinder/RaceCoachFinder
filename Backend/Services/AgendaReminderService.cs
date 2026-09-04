using Microsoft.EntityFrameworkCore;
using Backend.Data;

namespace Backend.Services;

public class AgendaReminderService : IHostedService, IDisposable
{
    private readonly IServiceProvider _services;
    private readonly ILogger<AgendaReminderService> _logger;
    private Timer? _timer;

    public AgendaReminderService(IServiceProvider services, ILogger<AgendaReminderService> logger)
    {
        _services = services;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken ct)
    {
        _timer = new Timer(VoerUit, null, TimeSpan.Zero, TimeSpan.FromHours(1));
        return Task.CompletedTask;
    }

    private async void VoerUit(object? state)
    {
        // Stuur dagelijkse reminders alleen om 7:00 UTC (≈ 8-9 uur Nederlandse tijd)
        if (DateTime.UtcNow.Hour != 7) return;

        try
        {
            using var scope = _services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var email = scope.ServiceProvider.GetRequiredService<IEmailService>();

            var vandaag = DateTime.UtcNow.Date;

            var instellingen = await context.GebruikerInstellingen
                .Where(i => i.AgendaReminderActief && i.EmailAan)
                .Where(i => i.LaatsteReminderDatum == null || i.LaatsteReminderDatum.Value.Date < vandaag)
                .ToListAsync();

            foreach (var inst in instellingen)
            {
                var doelDatum = vandaag.AddDays(inst.AgendaReminderDagenVanTevoren);

                var items = await context.AgendaItems
                    .Where(a => a.CoachGebruikerId == inst.GebruikerId && a.Datum.Date == doelDatum)
                    .OrderBy(a => a.Datum)
                    .ToListAsync();

                if (items.Count == 0) continue;

                var gebruiker = await context.Users.FindAsync(inst.GebruikerId);
                if (gebruiker == null || string.IsNullOrEmpty(gebruiker.Email)) continue;

                var dag = inst.AgendaReminderDagenVanTevoren == 1 ? "morgen" : $"over {inst.AgendaReminderDagenVanTevoren} dagen";
                var html = EmailTemplates.AgendaReminder(gebruiker.Naam, doelDatum, items);
                await email.VerstuurAsync(gebruiker.Email, gebruiker.Naam,
                    $"Agenda herinnering – {dag}: {items.Count} afspraak{(items.Count == 1 ? "" : "en")}", html);

                inst.LaatsteReminderDatum = DateTime.UtcNow;
                _logger.LogInformation("Agenda reminder verstuurd naar {Email} voor {Datum}", gebruiker.Email, doelDatum.ToShortDateString());
            }

            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fout bij versturen agenda reminders");
        }
    }

    public Task StopAsync(CancellationToken ct)
    {
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    public void Dispose() => _timer?.Dispose();
}
