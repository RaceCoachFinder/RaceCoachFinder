namespace Backend.Models;

public class Review
{
    public int Id { get; set; }
    public string CoachGebruikerId { get; set; } = string.Empty;
    public string RijderGebruikerId { get; set; } = string.Empty;
    public int Sterren { get; set; }
    public string Tekst { get; set; } = string.Empty;
    public DateTime AangemaaktOp { get; set; } = DateTime.UtcNow;
}
