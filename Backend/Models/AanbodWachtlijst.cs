namespace Backend.Models;

// Rijder wil een melding als er een plek vrijkomt bij een volle training
public class AanbodWachtlijst
{
    public int Id { get; set; }
    public int CoachAanbodId { get; set; }
    public string GebruikerId { get; set; } = string.Empty;
    public DateTime AangemaaktOp { get; set; } = DateTime.UtcNow;
}
