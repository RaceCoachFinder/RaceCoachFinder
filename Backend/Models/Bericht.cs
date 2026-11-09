namespace Backend.Models;

public class Bericht
{
    public int Id { get; set; }
    public string VanGebruikerId { get; set; } = string.Empty;
    public string NaarGebruikerId { get; set; } = string.Empty;
    public string Tekst { get; set; } = string.Empty;
    public DateTime AangemaaktOp { get; set; } = DateTime.UtcNow;
    public bool Gelezen { get; set; } = false;
    public DateTime? BewerktOp { get; set; }
    // 'Gesprek verwijderen' haalt het gesprek alleen bij jezelf weg
    public bool VerborgenVoorVan { get; set; }
    public bool VerborgenVoorNaar { get; set; }
}
