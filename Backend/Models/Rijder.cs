namespace Backend.Models;

public class Rijder
{
    public int Id { get; set; }
    public string? GebruikerId { get; set; }
    public string Naam { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Woonplaats { get; set; } = string.Empty;
    public string Nationaliteit { get; set; } = string.Empty;
    public string Talen { get; set; } = string.Empty;
    public string Bio { get; set; } = string.Empty;
    public int Leeftijd { get; set; }
    public string Niveau { get; set; } = string.Empty;
    public string Discipline { get; set; } = string.Empty;
    public string Doelen { get; set; } = string.Empty;
    public string BeschikbareBanen { get; set; } = string.Empty;
    public string FotoUrl { get; set; } = string.Empty;
    public string Instagram { get; set; } = string.Empty;
    public string Resultaten { get; set; } = string.Empty;
}
