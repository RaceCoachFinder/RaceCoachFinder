namespace Backend.Models;

public class Boeking
{
    public int Id { get; set; }
    public string CoachGebruikerId { get; set; } = string.Empty;
    public string RijderGebruikerId { get; set; } = string.Empty;
    public string Omschrijving { get; set; } = string.Empty;
    public decimal Bedrag { get; set; }
    public string Status { get; set; } = "Openstaand";
    public DateTime AangemaaktOp { get; set; } = DateTime.UtcNow;
    public bool CoachHeeftBevestigd { get; set; }
    public bool RijderHeeftBevestigd { get; set; }
    public string? FactuurnummerTekst { get; set; }
    public int BetalingsTermijn { get; set; } = 14;
    public string? FactuurJson { get; set; }
}
