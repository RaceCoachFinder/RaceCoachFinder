namespace Backend.Models;

public class Bericht
{
    public int Id { get; set; }
    public string VanGebruikerId { get; set; } = string.Empty;
    public string NaarGebruikerId { get; set; } = string.Empty;
    public string Tekst { get; set; } = string.Empty;
    public DateTime AangemaaktOp { get; set; } = DateTime.UtcNow;
    public bool Gelezen { get; set; } = false;
}
