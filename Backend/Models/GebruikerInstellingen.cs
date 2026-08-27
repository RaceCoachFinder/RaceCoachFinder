namespace Backend.Models;

public class GebruikerInstellingen
{
    public int Id { get; set; }
    public string GebruikerId { get; set; } = string.Empty;

    // E-mail notificaties
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
}
