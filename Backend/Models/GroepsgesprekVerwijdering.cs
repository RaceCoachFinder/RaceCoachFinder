namespace Backend.Models;

// Iemand die door de beheerder uit een groep is verwijderd en zich niet zelf opnieuw mag aanmelden
public class GroepsgesprekVerwijdering
{
    public int Id { get; set; }
    public int GroepsgesprekId { get; set; }
    public string GebruikerId { get; set; } = string.Empty;
    public DateTime VerwijderdOp { get; set; } = DateTime.UtcNow;
}
