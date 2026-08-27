using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models;

public class Coach
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
    public int JarenErvaring { get; set; }
    public string Discipline { get; set; } = string.Empty;
    public string Specialisatie { get; set; } = string.Empty;
    public string BehaaldResultaten { get; set; } = string.Empty;
    public string BeschikbareBanen { get; set; } = string.Empty;
    public bool OnlineCoaching { get; set; }
    public string PrijsPakketten { get; set; } = string.Empty;
    public bool IsGepubliceerd { get; set; } = false;
    public int AantalWeergaven { get; set; } = 0;
    public string FotoUrl { get; set; } = string.Empty;
    public string Instagram { get; set; } = string.Empty;
    public string Kwaliteiten { get; set; } = string.Empty;

    [NotMapped] public double GemiddeldeScore { get; set; }
    [NotMapped] public int AantalReviews { get; set; }
    [NotMapped] public double GemiddeldeReactietijdUren { get; set; } = -1;
}
