namespace Backend.Models;

// Algemene sleutel/waarde-instellingen van de app (bijv. de VAPID-sleutels voor pushmeldingen)
public class AppInstelling
{
    public int Id { get; set; }
    public string Sleutel { get; set; } = string.Empty;
    public string Waarde { get; set; } = string.Empty;
}
