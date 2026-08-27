namespace Backend.Models;

public class CoachFavoriet
{
    public int Id { get; set; }
    public string RijderId { get; set; } = string.Empty;
    public int CoachId { get; set; }
}
