namespace Backend.Models;

public class CoachUitnodiging
{
    public int Id { get; set; }
    public int CoachAanbodId { get; set; }
    public string AanbodTitel { get; set; } = string.Empty;
    public string RijderGebruikerId { get; set; } = string.Empty;
    public string RijderNaam { get; set; } = string.Empty;
    public string CoachGebruikerId { get; set; } = string.Empty;
    public string CoachNaam { get; set; } = string.Empty;
    public string Status { get; set; } = "Openstaand"; // Openstaand, Geaccepteerd, Afgewezen
    public decimal? Percentage { get; set; }
    public DateTime AangemaaktOp { get; set; } = DateTime.UtcNow;
}
