namespace Backend.Models;

public class Groepsgesprek
{
    public int Id { get; set; }
    public int CoachAanbodId { get; set; }
    public string Naam { get; set; } = string.Empty;
    public string AangemaaktDoorId { get; set; } = string.Empty;
    public DateTime AangemaaktOp { get; set; } = DateTime.UtcNow;
}
