namespace Backend.Models;

public class Groepsbericht
{
    public int Id { get; set; }
    public int GroepsgesprekId { get; set; }
    public string VanGebruikerId { get; set; } = string.Empty;
    public string VanNaam { get; set; } = string.Empty;
    public string Tekst { get; set; } = string.Empty;
    public DateTime AangemaaktOp { get; set; } = DateTime.UtcNow;
}
