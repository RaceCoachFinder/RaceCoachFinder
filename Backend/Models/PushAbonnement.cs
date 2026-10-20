namespace Backend.Models;

// Eén apparaat/browser van een gebruiker dat pushmeldingen wil ontvangen
public class PushAbonnement
{
    public int Id { get; set; }
    public string GebruikerId { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public string P256dh { get; set; } = string.Empty;
    public string Auth { get; set; } = string.Empty;
    public DateTime AangemaaktOp { get; set; } = DateTime.UtcNow;
}
