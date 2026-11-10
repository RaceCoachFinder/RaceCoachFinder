namespace Backend.Models;

// Koppeling van een coach met zijn eigen Mollie-account (Mollie Connect).
// Betalingen van rijders komen rechtstreeks bij de coach binnen; de servicekosten gaan als
// application fee automatisch naar RaceCoachFinder. De tokens staan versleuteld in de database.
public class MollieKoppeling
{
    public int Id { get; set; }
    public string GebruikerId { get; set; } = string.Empty;
    public string? OrganisatieNaam { get; set; }
    public string? ProfielId { get; set; }
    public string RefreshTokenVersleuteld { get; set; } = string.Empty;
    public string? AccessTokenVersleuteld { get; set; }
    public DateTime? AccessTokenVerlooptOp { get; set; }
    // Status van de aanmelding bij Mollie: needs-data, in-review of completed
    public string? OnboardingStatus { get; set; }
    public bool KanBetalingenOntvangen { get; set; }
    public DateTime GekoppeldOp { get; set; } = DateTime.UtcNow;
    public DateTime BijgewerktOp { get; set; } = DateTime.UtcNow;
}
