using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using Backend.Data;
using Backend.Models;

namespace Backend.Controllers;

// Trainingsvoorstel in de groepschat van een rijder-aanvraag ("Rijder zoekt groep").
// De coach die de chat startte (organisator) werkt de training uit, voegt eventueel coaches toe
// met een percentage en publiceert hem daarna als groepstraining. Rijders zien het voorstel en
// wie er coacht, maar niet de percentages.
[ApiController]
[Route("api/trainingvoorstel")]
[Authorize]
public class TrainingVoorstelController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private static readonly JsonSerializerOptions JsonOpties = new(JsonSerializerDefaults.Web);

    public TrainingVoorstelController(AppDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public class VoorstelCoach
    {
        public string Id { get; set; } = string.Empty;
        public string Naam { get; set; } = string.Empty;
        public int? Percentage { get; set; }
        public bool Akkoord { get; set; }
    }

    private string MijnId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    private static List<VoorstelCoach> Coaches(TrainingVoorstel v) =>
        JsonSerializer.Deserialize<List<VoorstelCoach>>(v.CoachesJson, JsonOpties) ?? new();

    private static void ZetCoaches(TrainingVoorstel v, List<VoorstelCoach> coaches) =>
        v.CoachesJson = JsonSerializer.Serialize(coaches, JsonOpties);

    // Groep + aanvraag + voorstel ophalen; maakt het voorstel aan (ingevuld vanuit de aanvraag) als het nog niet bestaat
    private async Task<(Groepsgesprek? groep, RijderAanvraag? aanvraag, TrainingVoorstel? voorstel, IActionResult? fout)> Laad(int groepId)
    {
        var groep = await _context.Groepsgesprekken.FindAsync(groepId);
        if (groep?.RijderAanvraagId == null) return (null, null, null, NotFound("Deze chat hoort niet bij een groepsverzoek."));
        if (!await _context.GroepsgesprekLeden.AnyAsync(l => l.GroepsgesprekId == groepId && l.GebruikerId == MijnId))
            return (null, null, null, StatusCode(403, "Je zit niet in deze groep."));

        var aanvraag = await _context.RijderAanvragen.FindAsync(groep.RijderAanvraagId.Value);
        if (aanvraag == null) return (null, null, null, NotFound("Groepsverzoek niet gevonden."));

        var voorstel = await _context.TrainingVoorstellen.FirstOrDefaultAsync(v => v.GroepsgesprekId == groepId);
        if (voorstel == null)
        {
            var isWedstrijd = aanvraag.SoortOpdracht == "Wedstrijd";
            var wedstrijdNaam = string.Join(" — ", new[] { aanvraag.WedstrijdOrganisatie, aanvraag.WedstrijdNaam }.Where(s => !string.IsNullOrWhiteSpace(s)));
            voorstel = new TrainingVoorstel
            {
                GroepsgesprekId = groepId,
                RijderAanvraagId = aanvraag.Id,
                OrganisatorId = groep.AangemaaktDoorId,
                Titel = isWedstrijd && wedstrijdNaam != "" ? wedstrijdNaam : (isWedstrijd ? "Wedstrijdbegeleiding" : "Groepscoaching"),
                Beschrijving = aanvraag.Beschrijving,
                Locatie = aanvraag.Locatie,
                Datum = aanvraag.Datum,
                MaxRijders = aanvraag.GroepGrootte,
                PrijsPerRijder = aanvraag.MaxPrijs,
                // Bij een wedstrijd staat het soort (Sprint/Endurance) vooraan in de kwaliteiten van de aanvraag
                Categorieen = isWedstrijd
                    ? string.Join(",", new[] { "Wedstrijddag" }.Concat(aanvraag.Kwaliteiten.Split(',').Select(k => k.Trim()).Where(k => k is "Sprint" or "Endurance").Take(1)))
                    : "Trainingsdag",
                Disciplines = aanvraag.Specialisaties,
                WedstrijdOrganisatie = isWedstrijd ? aanvraag.WedstrijdOrganisatie : "",
                WedstrijdNaam = isWedstrijd ? aanvraag.WedstrijdNaam : ""
            };
            _context.TrainingVoorstellen.Add(voorstel);
            await _context.SaveChangesAsync();
        }
        return (groep, aanvraag, voorstel, null);
    }

    private async Task Systeembericht(int groepId, string tekst)
    {
        _context.Groepsberichten.Add(new Groepsbericht
        {
            GroepsgesprekId = groepId,
            VanGebruikerId = MijnId,
            VanNaam = await MijnNaam(),
            Tekst = tekst,
            AangemaaktOp = DateTime.UtcNow
        });
    }

    private async Task<string> MijnNaam() => (await _userManager.FindByIdAsync(MijnId))?.Naam ?? "Coach";

    private async Task<object> Weergave(Groepsgesprek groep, RijderAanvraag aanvraag, TrainingVoorstel v)
    {
        var coaches = Coaches(v);
        var isOrganisator = v.OrganisatorId == MijnId;
        // Percentages alleen voor de organisator en de toegevoegde coaches
        var magPercentagesZien = isOrganisator || coaches.Any(c => c.Id == MijnId);
        var organisator = await _userManager.FindByIdAsync(v.OrganisatorId);
        return new
        {
            v.GroepsgesprekId,
            v.OrganisatorId,
            OrganisatorNaam = organisator?.Naam ?? "Coach",
            IsOrganisator = isOrganisator,
            MagPercentagesZien = magPercentagesZien,
            v.Titel, v.Beschrijving, v.Locatie, v.Datum, v.MaxRijders, v.PrijsPerRijder, v.IsGratis,
            v.Categorieen, v.Disciplines, v.WedstrijdOrganisatie, v.WedstrijdNaam,
            Coaches = coaches.Select(c => new { c.Id, c.Naam, Percentage = magPercentagesZien ? c.Percentage : null, c.Akkoord }),
            EigenAandeel = magPercentagesZien ? 100 - coaches.Sum(c => c.Percentage ?? 0) : (int?)null,
            AlleAkkoord = coaches.All(c => c.Akkoord),
            v.GepubliceerdAanbodId,
            Aanvraag = new { aanvraag.Id, aanvraag.RijderId, aanvraag.RijderNaam, aanvraag.GroepGrootte, aanvraag.SoortOpdracht }
        };
    }

    [HttpGet("groep/{groepId:int}")]
    public async Task<IActionResult> Get(int groepId)
    {
        var (groep, aanvraag, voorstel, fout) = await Laad(groepId);
        if (fout != null) return fout;
        return Ok(await Weergave(groep!, aanvraag!, voorstel!));
    }

    public record VoorstelVerzoek(string? Titel, string? Beschrijving, string? Locatie, DateTime? Datum,
        int? MaxRijders, decimal? PrijsPerRijder, bool IsGratis, string? Categorieen, string? Disciplines,
        string? WedstrijdOrganisatie = null, string? WedstrijdNaam = null);

    [HttpPut("groep/{groepId:int}")]
    public async Task<IActionResult> Bijwerken(int groepId, [FromBody] VoorstelVerzoek verzoek)
    {
        var (groep, aanvraag, v, fout) = await Laad(groepId);
        if (fout != null) return fout;
        if (v!.OrganisatorId != MijnId) return StatusCode(403, "Alleen de coach die de training organiseert kan hem aanpassen.");
        if (v.GepubliceerdAanbodId != null) return BadRequest("De training is al gepubliceerd. Pas hem aan via Groepscoaching.");
        if (verzoek.MaxRijders is < 1) return BadRequest("Maximaal aantal rijders moet minstens 1 zijn.");
        if (verzoek.PrijsPerRijder is < 0) return BadRequest("Prijs kan niet negatief zijn.");

        // Andere prijs = ander bedrag voor de coaches, dus opnieuw akkoord vragen
        var prijsAnders = v.PrijsPerRijder != verzoek.PrijsPerRijder || v.IsGratis != verzoek.IsGratis;
        v.Titel = verzoek.Titel?.Trim() ?? "";
        v.Beschrijving = verzoek.Beschrijving?.Trim() ?? "";
        v.Locatie = verzoek.Locatie?.Trim() ?? "";
        v.Datum = verzoek.Datum;
        v.MaxRijders = verzoek.MaxRijders;
        v.PrijsPerRijder = verzoek.IsGratis ? null : verzoek.PrijsPerRijder;
        v.IsGratis = verzoek.IsGratis;
        v.Categorieen = verzoek.Categorieen ?? "";
        v.Disciplines = verzoek.Disciplines ?? "";
        v.WedstrijdOrganisatie = verzoek.WedstrijdOrganisatie?.Trim() ?? "";
        v.WedstrijdNaam = verzoek.WedstrijdNaam?.Trim() ?? "";
        v.BijgewerktOp = DateTime.UtcNow;
        if (prijsAnders)
        {
            var coaches = Coaches(v);
            coaches.ForEach(c => c.Akkoord = false);
            ZetCoaches(v, coaches);
        }
        await Systeembericht(groepId, $"✏️ {await MijnNaam()} heeft de training aangepast. Bekijk hem via de knop 'Training' bovenaan.");
        await _context.SaveChangesAsync();
        return Ok(await Weergave(groep!, aanvraag!, v));
    }

    public record CoachVerzoek(string? CoachId, int? Percentage);

    [HttpPost("groep/{groepId:int}/coaches")]
    public async Task<IActionResult> CoachToevoegen(int groepId, [FromBody] CoachVerzoek verzoek)
    {
        var (groep, aanvraag, v, fout) = await Laad(groepId);
        if (fout != null) return fout;
        if (v!.OrganisatorId != MijnId) return StatusCode(403, "Alleen de organisator kan coaches toevoegen.");
        if (v.GepubliceerdAanbodId != null) return BadRequest("De training is al gepubliceerd.");

        var coach = await _userManager.FindByIdAsync(verzoek.CoachId ?? "");
        if (coach == null || !(coach.Rol ?? "").Split(',').Select(r => r.Trim()).Contains("Coach"))
            return NotFound("Coach niet gevonden.");
        if (coach.Id == v.OrganisatorId) return BadRequest("Je bent zelf al de organisator.");

        var coaches = Coaches(v);
        if (coaches.Any(c => c.Id == coach.Id)) return BadRequest($"{coach.Naam} is al toegevoegd.");
        var perc = verzoek.Percentage ?? 0;
        if (perc < 0 || perc > 100) return BadRequest("Percentage moet tussen 0 en 100 liggen.");
        if (coaches.Sum(c => c.Percentage ?? 0) + perc > 100) return BadRequest("De aandelen van de coaches komen samen boven de 100%.");

        coaches.Add(new VoorstelCoach { Id = coach.Id, Naam = coach.Naam, Percentage = perc, Akkoord = false });
        ZetCoaches(v, coaches);

        // De coach komt ook in de chat, zodat hij mee kan praten
        if (!await _context.GroepsgesprekLeden.AnyAsync(l => l.GroepsgesprekId == groepId && l.GebruikerId == coach.Id))
        {
            _context.GroepsgesprekLeden.Add(new GroepsgesprekLid
            {
                GroepsgesprekId = groepId, GebruikerId = coach.Id, GebruikerNaam = coach.Naam, ToegetreedOp = DateTime.UtcNow
            });
            _context.GroepsgesprekVerwijderingen.RemoveRange(
                _context.GroepsgesprekVerwijderingen.Where(x => x.GroepsgesprekId == groepId && x.GebruikerId == coach.Id));
        }
        await Systeembericht(groepId, $"➕ {coach.Naam} is als coach aan de training toegevoegd.");
        await _context.SaveChangesAsync();
        return Ok(await Weergave(groep!, aanvraag!, v));
    }

    [HttpPut("groep/{groepId:int}/coaches/{coachId}")]
    public async Task<IActionResult> PercentageWijzigen(int groepId, string coachId, [FromBody] CoachVerzoek verzoek)
    {
        var (groep, aanvraag, v, fout) = await Laad(groepId);
        if (fout != null) return fout;
        if (v!.OrganisatorId != MijnId) return StatusCode(403, "Alleen de organisator kan percentages aanpassen.");
        if (v.GepubliceerdAanbodId != null) return BadRequest("De training is al gepubliceerd.");

        var coaches = Coaches(v);
        var c = coaches.FirstOrDefault(x => x.Id == coachId);
        if (c == null) return NotFound("Coach staat niet in de training.");
        var perc = verzoek.Percentage ?? 0;
        if (perc < 0 || perc > 100) return BadRequest("Percentage moet tussen 0 en 100 liggen.");
        if (coaches.Where(x => x.Id != coachId).Sum(x => x.Percentage ?? 0) + perc > 100)
            return BadRequest("De aandelen van de coaches komen samen boven de 100%.");
        if (c.Percentage != perc) c.Akkoord = false; // nieuw aandeel = opnieuw akkoord geven
        c.Percentage = perc;
        ZetCoaches(v, coaches);
        await _context.SaveChangesAsync();
        return Ok(await Weergave(groep!, aanvraag!, v));
    }

    [HttpDelete("groep/{groepId:int}/coaches/{coachId}")]
    public async Task<IActionResult> CoachVerwijderen(int groepId, string coachId)
    {
        var (groep, aanvraag, v, fout) = await Laad(groepId);
        if (fout != null) return fout;
        if (v!.OrganisatorId != MijnId) return StatusCode(403, "Alleen de organisator kan coaches verwijderen.");
        if (v.GepubliceerdAanbodId != null) return BadRequest("De training is al gepubliceerd.");

        var coaches = Coaches(v);
        var c = coaches.FirstOrDefault(x => x.Id == coachId);
        if (c == null) return NotFound("Coach staat niet in de training.");
        coaches.Remove(c);
        ZetCoaches(v, coaches);
        await Systeembericht(groepId, $"➖ {c.Naam} coacht niet meer mee met deze training.");
        await _context.SaveChangesAsync();
        return Ok(await Weergave(groep!, aanvraag!, v));
    }

    public record AkkoordVerzoek(bool Akkoord);

    // Een toegevoegde coach geeft aan dat hij akkoord is met de training en zijn aandeel
    [HttpPost("groep/{groepId:int}/akkoord")]
    public async Task<IActionResult> Akkoord(int groepId, [FromBody] AkkoordVerzoek verzoek)
    {
        var (groep, aanvraag, v, fout) = await Laad(groepId);
        if (fout != null) return fout;
        if (v!.GepubliceerdAanbodId != null) return BadRequest("De training is al gepubliceerd.");
        var coaches = Coaches(v);
        var c = coaches.FirstOrDefault(x => x.Id == MijnId);
        if (c == null) return StatusCode(403, "Je bent niet als coach aan deze training toegevoegd.");
        if (c.Akkoord != verzoek.Akkoord)
        {
            c.Akkoord = verzoek.Akkoord;
            ZetCoaches(v, coaches);
            await Systeembericht(groepId, verzoek.Akkoord
                ? $"✅ {c.Naam} is akkoord met de training."
                : $"↩️ {c.Naam} is (nog) niet akkoord met de training.");
            await _context.SaveChangesAsync();
        }
        return Ok(await Weergave(groep!, aanvraag!, v));
    }

    // Publiceren: maakt er een groepstraining van op "Coach biedt aan". De chat wordt de chat van die training.
    [HttpPost("groep/{groepId:int}/publiceer")]
    public async Task<IActionResult> Publiceer(int groepId)
    {
        var (groep, aanvraag, v, fout) = await Laad(groepId);
        if (fout != null) return fout;
        if (v!.OrganisatorId != MijnId) return StatusCode(403, "Alleen de organisator kan de training publiceren.");
        if (v.GepubliceerdAanbodId != null) return BadRequest("De training is al gepubliceerd.");
        if (string.IsNullOrWhiteSpace(v.Titel)) return BadRequest("Geef de training een titel.");
        if (string.IsNullOrWhiteSpace(v.Locatie)) return BadRequest("Vul een locatie in.");
        if (v.Datum == null) return BadRequest("Kies een datum.");
        if (v.Datum.Value.Date < DateTime.UtcNow.Date) return BadRequest("De datum moet in de toekomst liggen.");
        var coaches = Coaches(v);
        var nietAkkoord = coaches.Where(c => !c.Akkoord).Select(c => c.Naam).ToList();
        if (nietAkkoord.Any()) return BadRequest($"Nog niet akkoord: {string.Join(", ", nietAkkoord)}.");
        if (coaches.Sum(c => c.Percentage ?? 0) > 100) return BadRequest("De aandelen van de coaches komen samen boven de 100%.");

        var organisator = await _userManager.FindByIdAsync(v.OrganisatorId);
        var aanbod = new CoachAanbod
        {
            CoachGebruikerId = v.OrganisatorId,
            CoachNaam = organisator?.Naam ?? "Coach",
            Titel = v.Titel.Trim(),
            Beschrijving = v.Beschrijving,
            Datum = v.Datum.Value,
            Locatie = v.Locatie.Trim(),
            Categorieen = v.Categorieen,
            Disciplines = v.Disciplines,
            WedstrijdOrganisatie = v.WedstrijdOrganisatie,
            WedstrijdNaam = v.WedstrijdNaam,
            MaxRijders = v.MaxRijders,
            PrijsPerRijder = v.IsGratis ? null : v.PrijsPerRijder,
            IsGratis = v.IsGratis,
            IsGroep = true,
            IsActief = true,
            WachtOpCoaches = false,
            AangemaaktOp = DateTime.UtcNow
        };
        _context.CoachAanboden.Add(aanbod);
        await _context.SaveChangesAsync();

        // Coaches hebben al akkoord gegeven in de chat, dus hun uitnodiging is meteen geaccepteerd
        foreach (var c in coaches)
        {
            _context.CoachAanbodUitnodigingen.Add(new CoachUitnodiging
            {
                CoachAanbodId = aanbod.Id,
                AanbodTitel = aanbod.Titel,
                RijderGebruikerId = v.OrganisatorId,
                RijderNaam = aanbod.CoachNaam,
                CoachGebruikerId = c.Id,
                CoachNaam = c.Naam,
                Percentage = c.Percentage,
                Status = "Geaccepteerd",
                AangemaaktOp = DateTime.UtcNow
            });
        }

        // De bestaande chat wordt de chat van de training: rijders die er al in zitten doen mee
        groep!.CoachAanbodId = aanbod.Id;
        groep.Naam = aanbod.Titel;
        v.GepubliceerdAanbodId = aanbod.Id;
        aanvraag!.IsGesloten = true;
        aanvraag.CoachGevonden = true;
        await Systeembericht(groepId, $"🎉 De training '{aanbod.Titel}' is gepubliceerd en staat nu bij Groepscoaching → Coach biedt aan.");
        await _context.SaveChangesAsync();
        return Ok(new { aanbodId = aanbod.Id });
    }
}
