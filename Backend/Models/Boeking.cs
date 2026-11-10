namespace Backend.Models;

public class Boeking
{
    public int Id { get; set; }
    public string CoachGebruikerId { get; set; } = string.Empty;
    public string RijderGebruikerId { get; set; } = string.Empty;
    public string Omschrijving { get; set; } = string.Empty;
    public decimal Bedrag { get; set; }
    public string Status { get; set; } = "Openstaand";
    public DateTime AangemaaktOp { get; set; } = DateTime.UtcNow;
    public bool CoachHeeftBevestigd { get; set; }
    public bool RijderHeeftBevestigd { get; set; }
    public string? FactuurnummerTekst { get; set; }
    public int BetalingsTermijn { get; set; } = 14;
    public string? FactuurJson { get; set; }
    public string? MollieBetalingId { get; set; }
    // Waar de betaling staat: null = Mollie-account van RaceCoachFinder (oude facturen),
    // "koppeling" = Mollie-account van de coach, "koppeling-test" = idem in testmodus
    public string? MollieModus { get; set; }
    // Servicekosten (2%) die via Mollie naar RaceCoachFinder gaan
    public decimal? Servicekosten { get; set; }

    // "online" (via Mollie; ook als leeg bij oude facturen) of "overmaken" (coach zonder Mollie)
    public string? BetaalWijze { get; set; }
    // Bij overmaken: rekeningnummer van de coach op het moment van de factuur, en eventueel een betaalverzoek-link
    public string? Iban { get; set; }
    public string? IbanTenaamstelling { get; set; }
    public string? BetaalLink { get; set; }
    // Rijder heeft aangegeven dat hij heeft overgemaakt (de coach bevestigt met "Ontvangen")
    public DateTime? RijderMeldtBetaaldOp { get; set; }
}
