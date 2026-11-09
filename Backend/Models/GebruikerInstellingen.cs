using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models;

public class GebruikerInstellingen
{
    public int Id { get; set; }
    public string GebruikerId { get; set; } = string.Empty;

    // E-mail notificaties
    public string EmailTaal { get; set; } = "nl";        // taal van alle e-mails: "nl" of "en"
    public bool EmailAan { get; set; } = true;
    public int BerichtenDrempel { get; set; } = 1;     // Stuur mail na hoeveel gemiste berichten
    public bool AlleenFavorieten { get; set; } = false; // Alleen van favoriete rijders/coaches
    public bool EmailBijBetaalUpdate { get; set; } = true;
    public bool EmailBijNieuweAanvraag { get; set; } = true; // Coach: nieuwe passende aanvraag
    public bool EmailBijNieuweReview { get; set; } = true;   // Coach: nieuwe review ontvangen

    // Privacy
    public bool ProfielOpenbaar { get; set; } = true;   // Of je profiel vindbaar is in zoekopdracht

    // Berichten
    public bool BerichtenVanOnbekenden { get; set; } = true; // Berichten van niet-favorieten toestaan

    // Agenda reminders
    public bool AgendaReminderActief { get; set; } = false;
    public int AgendaReminderDagenVanTevoren { get; set; } = 1; // 1, 2, 3 of 7
    public DateTime? LaatsteReminderDatum { get; set; }

    // Staat op het account (ApplicationUser), alleen hier om mee te sturen naar/van de instellingenpagina
    [NotMapped] public bool? MarketingToestemming { get; set; }
}
