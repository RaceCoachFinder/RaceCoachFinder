namespace Backend.Models;

public class RijderFavoriet
{
    public int Id { get; set; }
    public string CoachId { get; set; } = string.Empty;
    public string RijderId { get; set; } = string.Empty;
}
