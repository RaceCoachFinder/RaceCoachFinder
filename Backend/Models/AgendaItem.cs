namespace Backend.Models;

public class AgendaItem
{
    public int Id { get; set; }
    public string CoachGebruikerId { get; set; } = string.Empty;
    public string Titel { get; set; } = string.Empty;
    public DateTime Datum { get; set; }
    public string? Notitie { get; set; }
    public int? GekoppeldeBoekingId { get; set; }
}
