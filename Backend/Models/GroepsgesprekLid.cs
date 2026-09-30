namespace Backend.Models;

public class GroepsgesprekLid
{
    public int Id { get; set; }
    public int GroepsgesprekId { get; set; }
    public string GebruikerId { get; set; } = string.Empty;
    public string GebruikerNaam { get; set; } = string.Empty;
    public DateTime ToegetreedOp { get; set; } = DateTime.UtcNow;
    public DateTime? LaatstGelezen { get; set; }
}
