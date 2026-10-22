namespace Backend.Models;

// Concept van een groepstraining dat een coach in de groepschat van een rijder-aanvraag uitwerkt.
// Na publiceren wordt het een gewone CoachAanbod (groepstraining) en blijft de chat de chat van die training.
public class TrainingVoorstel
{
    public int Id { get; set; }
    public int GroepsgesprekId { get; set; }
    public int RijderAanvraagId { get; set; }
    public string OrganisatorId { get; set; } = string.Empty;
    public string Titel { get; set; } = string.Empty;
    public string Beschrijving { get; set; } = string.Empty;
    public string Locatie { get; set; } = string.Empty;
    public DateTime? Datum { get; set; }
    public int? MaxRijders { get; set; }
    public decimal? PrijsPerRijder { get; set; }
    public bool IsGratis { get; set; } = false;
    public string Categorieen { get; set; } = string.Empty;   // comma-separated
    public string Disciplines { get; set; } = string.Empty;   // comma-separated
    public string WedstrijdOrganisatie { get; set; } = string.Empty;
    public string WedstrijdNaam { get; set; } = string.Empty;
    // Extra coaches: JSON-lijst van { id, naam, percentage, akkoord }
    public string CoachesJson { get; set; } = "[]";
    public int? GepubliceerdAanbodId { get; set; }
    public DateTime BijgewerktOp { get; set; } = DateTime.UtcNow;
}
