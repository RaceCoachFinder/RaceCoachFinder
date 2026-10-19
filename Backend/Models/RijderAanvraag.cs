namespace Backend.Models;

public class RijderAanvraag
{
    public int Id { get; set; }
    public string RijderId { get; set; } = string.Empty;
    public string RijderNaam { get; set; } = string.Empty;
    public string Locatie { get; set; } = string.Empty;
    public DateTime? Datum { get; set; }
    public decimal? MaxPrijs { get; set; }
    public string Beschrijving { get; set; } = string.Empty;
    public string Specialisaties { get; set; } = string.Empty;
    public string Kwaliteiten { get; set; } = string.Empty;
    public DateTime AangemaaktOp { get; set; } = DateTime.UtcNow;
    public bool IsGesloten { get; set; } = false;
    public bool CoachGevonden { get; set; } = false;
    public string SoortOpdracht { get; set; } = "Training"; // Training | Wedstrijd
    public bool IsGroep { get; set; } = false;
    public int? GroepGrootte { get; set; }
    public string WedstrijdNaam { get; set; } = string.Empty;
    public string WedstrijdOrganisatie { get; set; } = string.Empty;
    public string UitgenodigdeRijders { get; set; } = "[]";
}
