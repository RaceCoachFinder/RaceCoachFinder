namespace Backend.Models;

public class CoachAanbod
{
    public int Id { get; set; }
    public string CoachGebruikerId { get; set; } = string.Empty;
    public string CoachNaam { get; set; } = string.Empty;
    public string Titel { get; set; } = string.Empty;
    public string Beschrijving { get; set; } = string.Empty;
    public DateTime Datum { get; set; }
    public string Locatie { get; set; } = string.Empty;
    public string Categorieen { get; set; } = string.Empty;   // comma-separated
    public string Disciplines { get; set; } = string.Empty;   // comma-separated
    public int? MaxRijders { get; set; }
    public decimal? PrijsPerRijder { get; set; }
    public bool IsGratis { get; set; } = false;
    public DateTime AangemaaktOp { get; set; } = DateTime.UtcNow;
    public bool IsActief { get; set; } = true;
}
