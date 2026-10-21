using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Backend.Models;
using System.Security.Claims;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CoachAanbodController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public CoachAanbodController(AppDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAlle(
        [FromQuery] string? categorie,
        [FromQuery] string? discipline,
        [FromQuery] string? locatie,
        [FromQuery] DateTime? datumVan,
        [FromQuery] bool? gratis,
        [FromQuery] bool? groep = null)
    {
        var nu = DateTime.UtcNow.Date;
        var query = _context.CoachAanboden
            .Where(a => a.IsActief && a.Datum.Date >= nu);

        if (groep.HasValue)
            query = query.Where(a => a.IsGroep == groep.Value);

        if (!string.IsNullOrWhiteSpace(categorie))
            query = query.Where(a => a.Categorieen.Contains(categorie));

        if (!string.IsNullOrWhiteSpace(discipline))
            query = query.Where(a => a.Disciplines.Contains(discipline));

        if (!string.IsNullOrWhiteSpace(locatie))
            query = query.Where(a => a.Locatie.ToLower().Contains(locatie.ToLower()));

        if (datumVan.HasValue)
            query = query.Where(a => a.Datum.Date >= datumVan.Value.Date);

        if (gratis.HasValue)
            query = query.Where(a => a.IsGratis == gratis.Value);

        var lijst = await query.OrderBy(a => a.Datum).ToListAsync();

        // Voeg CoachProfielId + review-stats toe via join op GebruikerId
        var aanbodIdsVoorProfielen = lijst.Select(a => a.Id).ToList();
        var uitgenodigdeIds = await _context.CoachAanbodUitnodigingen
            .Where(u => aanbodIdsVoorProfielen.Contains(u.CoachAanbodId) && u.Status != "Afgewezen")
            .Select(u => u.CoachGebruikerId)
            .ToListAsync();
        var gebruikerIds = lijst.Select(a => a.CoachGebruikerId).Distinct().ToList();
        var profielGebruikerIds = gebruikerIds.Concat(uitgenodigdeIds).Distinct().ToList();

        var profielIds = await _context.Coaches
            .Where(c => c.GebruikerId != null && profielGebruikerIds.Contains(c.GebruikerId))
            .ToDictionaryAsync(c => c.GebruikerId!, c => c.Id);

        var reviewStats = await _context.Reviews
            .Where(r => gebruikerIds.Contains(r.CoachGebruikerId))
            .GroupBy(r => r.CoachGebruikerId)
            .Select(g => new { CoachGebruikerId = g.Key, Gemiddeld = g.Average(r => (double)r.Sterren), Aantal = g.Count() })
            .ToDictionaryAsync(x => x.CoachGebruikerId);

        // Groepsgesprekken + geaccepteerde leden per aanbod
        var aanbodIds = lijst.Select(a => a.Id).ToList();
        var groepen = await _context.Groepsgesprekken
            .Where(g => aanbodIds.Contains(g.CoachAanbodId))
            .ToDictionaryAsync(g => g.CoachAanbodId);

        var groepIds = groepen.Values.Select(g => g.Id).ToList();
        var alleLeden = await _context.GroepsgesprekLeden
            .Where(l => groepIds.Contains(l.GroepsgesprekId))
            .ToListAsync();

        // Uitgenodigde coaches (ook nog niet geaccepteerd)
        var alleUitnodigingen = await _context.CoachAanbodUitnodigingen
            .Where(u => aanbodIds.Contains(u.CoachAanbodId) && u.Status != "Afgewezen")
            .ToListAsync();

        var resultaat = lijst.Select(a => {
            groepen.TryGetValue(a.Id, out var groep);
            // Alleen uitgenodigde coaches tonen (geen rijders die via 'Reageren' meedoen)
            var alleCoachNamen = alleUitnodigingen
                .Where(u => u.CoachAanbodId == a.Id)
                .Select(u => u.CoachNaam)
                .Distinct()
                .ToList();
            return new {
                a.Id,
                a.CoachGebruikerId,
                CoachProfielId = profielIds.TryGetValue(a.CoachGebruikerId, out var pid) ? pid : (int?)null,
                a.CoachNaam,
                a.Titel,
                a.KorteOmschrijving,
                a.Beschrijving,
                a.Datum,
                a.Locatie,
                a.Categorieen,
                a.Disciplines,
                a.MaxRijders,
                a.PrijsPerRijder,
                a.IsGratis,
                a.AangemaaktOp,
                a.IsActief,
                GemiddeldeScore = reviewStats.TryGetValue(a.CoachGebruikerId, out var rs) ? rs.Gemiddeld : 0,
                AantalReviews = reviewStats.TryGetValue(a.CoachGebruikerId, out var rs2) ? rs2.Aantal : 0,
                GroepsgesprekId = groep?.Id,
                GeaccepteerdeDeelnemers = alleCoachNamen,
                // Organisator + uitgenodigde coaches, met profiel-id zodat de namen klikbaar zijn
                Coaches = new[] { new { Naam = a.CoachNaam, ProfielId = profielIds.TryGetValue(a.CoachGebruikerId, out var opid) ? opid : (int?)null, IsOrganisator = true } }
                    .Concat(alleUitnodigingen
                        .Where(u => u.CoachAanbodId == a.Id && u.CoachGebruikerId != a.CoachGebruikerId)
                        .GroupBy(u => u.CoachGebruikerId).Select(g => g.First())
                        .Select(u => new { Naam = u.CoachNaam, ProfielId = profielIds.TryGetValue(u.CoachGebruikerId, out var upid) ? upid : (int?)null, IsOrganisator = false }))
                    .ToList()
            };
        });

        return Ok(resultaat);
    }

    [HttpGet("mijn")]
    [Authorize(Roles = "Coach")]
    public async Task<IActionResult> GetMijn()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var lijst = await _context.CoachAanboden
            .Where(a => a.CoachGebruikerId == userId)
            .OrderByDescending(a => a.AangemaaktOp)
            .ToListAsync();

        var aanbodIds = lijst.Select(a => a.Id).ToList();
        var uitnodigingen = await _context.CoachAanbodUitnodigingen
            .Where(u => aanbodIds.Contains(u.CoachAanbodId))
            .ToListAsync();

        var resultaat = lijst.Select(a => new {
            a.Id, a.Titel, a.Datum, a.Locatie, a.Categorieen, a.Disciplines,
            a.MaxRijders, a.PrijsPerRijder, a.IsGratis, a.IsActief, a.WachtOpCoaches,
            a.KorteOmschrijving, a.Beschrijving,
            Uitnodigingen = uitnodigingen
                .Where(u => u.CoachAanbodId == a.Id)
                .Select(u => new { u.Id, u.CoachGebruikerId, u.CoachNaam, u.Status, u.Percentage })
                .ToList()
        });

        return Ok(resultaat);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Coach")]
    public async Task<IActionResult> Bewerken(int id, [FromBody] CoachAanbodVerzoek verzoek)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var gebruiker = await _userManager.FindByIdAsync(userId!);
        if (gebruiker == null) return Unauthorized();

        var aanbod = await _context.CoachAanboden.FindAsync(id);
        if (aanbod == null) return NotFound();
        if (aanbod.CoachGebruikerId != userId) return Forbid();

        aanbod.Titel = verzoek.Titel.Trim();
        aanbod.KorteOmschrijving = verzoek.KorteOmschrijving?.Trim() ?? string.Empty;
        aanbod.Beschrijving = verzoek.Beschrijving?.Trim() ?? string.Empty;
        aanbod.Datum = verzoek.Datum;
        aanbod.Locatie = verzoek.Locatie.Trim();
        aanbod.Categorieen = verzoek.Categorieen ?? string.Empty;
        aanbod.Disciplines = verzoek.Disciplines ?? string.Empty;
        aanbod.MaxRijders = verzoek.MaxRijders;
        aanbod.PrijsPerRijder = verzoek.PrijsPerRijder;
        aanbod.IsGratis = verzoek.IsGratis;

        // Zet afgewezen coaches terug op Openstaand en stuur opnieuw bericht
        var afgewezen = await _context.CoachAanbodUitnodigingen
            .Where(u => u.CoachAanbodId == id && u.Status == "Afgewezen")
            .ToListAsync();

        foreach (var u in afgewezen)
        {
            u.Status = "Openstaand";
            var datumStr = aanbod.Datum.ToString("d MMM yyyy");
            var prijsStr = aanbod.IsGratis ? "Gratis" : (aanbod.PrijsPerRijder.HasValue ? $"€{aanbod.PrijsPerRijder} p.p." : "");
            _context.Berichten.Add(new Bericht
            {
                VanGebruikerId = userId!,
                NaarGebruikerId = u.CoachGebruikerId,
                Tekst = $"🎯 UITNODIGING:{u.Id}\n{gebruiker.Naam} heeft de training aangepast en nodigt je opnieuw uit: {aanbod.Titel}\n📍 {aanbod.Locatie} | 📅 {datumStr}{(prijsStr != "" ? " | " + prijsStr : "")}",
                AangemaaktOp = DateTime.UtcNow,
                Gelezen = false
            });
        }

        // Hercheck of alle uitnodigingen nu geaccepteerd zijn
        var openstaand = await _context.CoachAanbodUitnodigingen
            .Where(u => u.CoachAanbodId == id && u.Status == "Openstaand")
            .CountAsync();
        aanbod.WachtOpCoaches = openstaand > 0 || afgewezen.Count > 0;
        aanbod.IsActief = !aanbod.WachtOpCoaches;

        await _context.SaveChangesAsync();
        return Ok(new { aanbod.Id, aanbod.IsActief, aanbod.WachtOpCoaches });
    }

    [HttpPost]
    [Authorize(Roles = "Coach")]
    public async Task<IActionResult> Aanmaken([FromBody] CoachAanbodVerzoek verzoek)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var gebruiker = await _userManager.FindByIdAsync(userId!);
        if (gebruiker == null) return Unauthorized();

        if (string.IsNullOrWhiteSpace(verzoek.Titel))
            return BadRequest("Titel is verplicht.");
        if (string.IsNullOrWhiteSpace(verzoek.Locatie))
            return BadRequest("Locatie is verplicht.");
        if (verzoek.Datum == default)
            return BadRequest("Datum is verplicht.");
        if (verzoek.Datum.Date < DateTime.UtcNow.Date)
            return BadRequest("Datum moet in de toekomst liggen.");

        var percentages = (verzoek.UitgenodigdeCoaches ?? new List<UitgenodigdeCoachVerzoek>()).Select(c => c.Percentage ?? 0).ToList();
        if (percentages.Any(p => p < 0 || p > 100))
            return BadRequest("Percentage moet tussen 0 en 100 liggen.");
        if (percentages.Sum() > 100)
            return BadRequest("De aandelen van de coaches komen samen boven de 100%.");

        var heeftUitnodigingen = verzoek.UitgenodigdeCoaches?.Any() == true;
        var aanbod = new CoachAanbod
        {
            CoachGebruikerId = userId!,
            CoachNaam = gebruiker.Naam,
            Titel = verzoek.Titel.Trim(),
            KorteOmschrijving = verzoek.KorteOmschrijving?.Trim() ?? string.Empty,
            Beschrijving = verzoek.Beschrijving?.Trim() ?? string.Empty,
            Datum = verzoek.Datum,
            Locatie = verzoek.Locatie.Trim(),
            Categorieen = verzoek.Categorieen ?? string.Empty,
            Disciplines = verzoek.Disciplines ?? string.Empty,
            MaxRijders = verzoek.MaxRijders,
            PrijsPerRijder = verzoek.PrijsPerRijder,
            IsGratis = verzoek.IsGratis,
            IsGroep = verzoek.IsGroep,
            AangemaaktOp = DateTime.UtcNow,
            IsActief = !heeftUitnodigingen,
            WachtOpCoaches = heeftUitnodigingen
        };

        _context.CoachAanboden.Add(aanbod);
        await _context.SaveChangesAsync();

        // Maak groepsgesprek aan voor deze aanbieding
        var groep = new Groepsgesprek
        {
            CoachAanbodId = aanbod.Id,
            Naam = aanbod.Titel,
            AangemaaktDoorId = userId!,
            AangemaaktOp = DateTime.UtcNow
        };
        _context.Groepsgesprekken.Add(groep);
        await _context.SaveChangesAsync();

        // Voeg maker toe als lid
        _context.GroepsgesprekLeden.Add(new GroepsgesprekLid
        {
            GroepsgesprekId = groep.Id,
            GebruikerId = userId!,
            GebruikerNaam = gebruiker.Naam,
            ToegetreedOp = DateTime.UtcNow
        });

        // Verwerk uitgenodigde coaches
        if (verzoek.UitgenodigdeCoaches != null)
        {
            var gezienIds = new HashSet<string>();
            foreach (var uitnodigingVerzoek in verzoek.UitgenodigdeCoaches)
            {
                if (!gezienIds.Add(uitnodigingVerzoek.Id)) continue;
                var coach = await _userManager.FindByIdAsync(uitnodigingVerzoek.Id);
                if (coach == null) continue;

                var uitnodiging = new CoachUitnodiging
                {
                    CoachAanbodId = aanbod.Id,
                    AanbodTitel = aanbod.Titel,
                    RijderGebruikerId = userId!,
                    RijderNaam = gebruiker.Naam,
                    CoachGebruikerId = uitnodigingVerzoek.Id,
                    CoachNaam = coach.Naam,
                    Percentage = uitnodigingVerzoek.Percentage,
                    Status = "Openstaand",
                    AangemaaktOp = DateTime.UtcNow
                };
                _context.CoachAanbodUitnodigingen.Add(uitnodiging);
                await _context.SaveChangesAsync();

                var datumStr = aanbod.Datum.ToString("d MMM yyyy");
                var prijsStr = aanbod.IsGratis ? "Gratis" : (aanbod.PrijsPerRijder.HasValue ? $"€{aanbod.PrijsPerRijder} p.p." : "");
                var percStr = uitnodigingVerzoek.Percentage.HasValue ? $"\n💰 Jouw aandeel: {uitnodigingVerzoek.Percentage}%" : "";
                _context.Berichten.Add(new Bericht
                {
                    VanGebruikerId = userId!,
                    NaarGebruikerId = uitnodigingVerzoek.Id,
                    Tekst = $"🎯 UITNODIGING:{uitnodiging.Id}\n{gebruiker.Naam} nodigt je uit voor: {aanbod.Titel}\n📍 {aanbod.Locatie} | 📅 {datumStr}{(prijsStr != "" ? " | " + prijsStr : "")}{percStr}",
                    AangemaaktOp = DateTime.UtcNow,
                    Gelezen = false
                });
            }
        }

        await _context.SaveChangesAsync();
        return Ok(new { aanbod.Id, groepsgesprekId = groep.Id });
    }

    // ── Coaches van een bestaande training beheren (alleen de maker) ──

    private static string UitnodigingTekst(CoachAanbod aanbod, CoachUitnodiging u, string makerNaam, string inleiding)
    {
        var datumStr = aanbod.Datum.ToString("d MMM yyyy");
        var prijsStr = aanbod.IsGratis ? "Gratis" : (aanbod.PrijsPerRijder.HasValue ? $"€{aanbod.PrijsPerRijder} p.p." : "");
        var percStr = u.Percentage.HasValue ? $"\n💰 Jouw aandeel: {u.Percentage}%" : "";
        return $"🎯 UITNODIGING:{u.Id}\n{makerNaam} {inleiding}: {aanbod.Titel}\n📍 {aanbod.Locatie} | 📅 {datumStr}{(prijsStr != "" ? " | " + prijsStr : "")}{percStr}";
    }

    // Training staat pas online als alle uitgenodigde coaches geaccepteerd hebben
    private async Task HerberekenStatus(CoachAanbod aanbod, string mijnId, string mijnNaam)
    {
        var nietGeaccepteerd = await _context.CoachAanbodUitnodigingen
            .AnyAsync(u => u.CoachAanbodId == aanbod.Id && u.Status != "Geaccepteerd");
        var wasWachtend = aanbod.WachtOpCoaches;
        aanbod.WachtOpCoaches = nietGeaccepteerd;
        aanbod.IsActief = !nietGeaccepteerd;

        if (wasWachtend && !nietGeaccepteerd)
        {
            var groep = await _context.Groepsgesprekken.FirstOrDefaultAsync(g => g.CoachAanbodId == aanbod.Id);
            if (groep != null)
                _context.Groepsberichten.Add(new Groepsbericht
                {
                    GroepsgesprekId = groep.Id, VanGebruikerId = mijnId, VanNaam = mijnNaam,
                    Tekst = "🎉 Alle coaches hebben geaccepteerd. De training staat nu online!",
                    AangemaaktOp = DateTime.UtcNow
                });
        }
    }

    private async Task<string?> ControleerPercentages(int aanbodId, decimal? nieuw, int? behalveUitnodigingId)
    {
        if (nieuw.HasValue && (nieuw < 0 || nieuw > 100)) return "Percentage moet tussen 0 en 100 liggen.";
        var anderen = await _context.CoachAanbodUitnodigingen
            .Where(u => u.CoachAanbodId == aanbodId && (behalveUitnodigingId == null || u.Id != behalveUitnodigingId))
            .SumAsync(u => u.Percentage ?? 0);
        if (anderen + (nieuw ?? 0) > 100)
            return $"Samen komen de percentages boven de 100% (andere coaches hebben al {anderen:0.##}%).";
        return null;
    }

    [HttpPost("{id:int}/coaches")]
    [Authorize(Roles = "Coach")]
    public async Task<IActionResult> CoachToevoegen(int id, [FromBody] UitgenodigdeCoachVerzoek verzoek)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var maker = await _userManager.FindByIdAsync(userId!);
        var aanbod = await _context.CoachAanboden.FindAsync(id);
        if (aanbod == null || maker == null) return NotFound();
        if (aanbod.CoachGebruikerId != userId) return Forbid();
        if (verzoek.Id == userId) return BadRequest("Je kunt jezelf niet uitnodigen.");

        var coach = await _userManager.FindByIdAsync(verzoek.Id ?? "");
        if (coach == null || !coach.Rol.Split(',').Any(r => r.Trim() == "Coach"))
            return BadRequest("Coach niet gevonden.");
        if (await _context.CoachAanbodUitnodigingen.AnyAsync(u => u.CoachAanbodId == id && u.CoachGebruikerId == coach.Id))
            return BadRequest($"{coach.Naam} is al uitgenodigd voor deze training.");
        var fout = await ControleerPercentages(id, verzoek.Percentage, null);
        if (fout != null) return BadRequest(fout);

        var u = new CoachUitnodiging
        {
            CoachAanbodId = id, AanbodTitel = aanbod.Titel,
            RijderGebruikerId = userId!, RijderNaam = maker.Naam,
            CoachGebruikerId = coach.Id, CoachNaam = coach.Naam,
            Percentage = verzoek.Percentage, Status = "Openstaand", AangemaaktOp = DateTime.UtcNow
        };
        _context.CoachAanbodUitnodigingen.Add(u);
        await _context.SaveChangesAsync();

        _context.Berichten.Add(new Bericht
        {
            VanGebruikerId = userId!, NaarGebruikerId = coach.Id,
            Tekst = UitnodigingTekst(aanbod, u, maker.Naam, "nodigt je uit voor"),
            AangemaaktOp = DateTime.UtcNow, Gelezen = false
        });
        await HerberekenStatus(aanbod, userId!, maker.Naam);
        await _context.SaveChangesAsync();
        return Ok(new { u.Id, u.CoachNaam, u.Status, u.Percentage });
    }

    [HttpPut("{id:int}/coaches/{uitnodigingId:int}")]
    [Authorize(Roles = "Coach")]
    public async Task<IActionResult> CoachPercentageWijzigen(int id, int uitnodigingId, [FromBody] PercentageVerzoek verzoek)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var maker = await _userManager.FindByIdAsync(userId!);
        var aanbod = await _context.CoachAanboden.FindAsync(id);
        if (aanbod == null || maker == null) return NotFound();
        if (aanbod.CoachGebruikerId != userId) return Forbid();
        var u = await _context.CoachAanbodUitnodigingen.FirstOrDefaultAsync(x => x.Id == uitnodigingId && x.CoachAanbodId == id);
        if (u == null) return NotFound("Uitnodiging niet gevonden.");
        if (u.Percentage == verzoek.Percentage) return Ok(new { u.Id, u.Status, u.Percentage });
        var fout = await ControleerPercentages(id, verzoek.Percentage, u.Id);
        if (fout != null) return BadRequest(fout);

        // Ander aandeel = andere afspraak: de coach moet opnieuw accepteren
        u.Percentage = verzoek.Percentage;
        u.Status = "Openstaand";
        _context.Berichten.Add(new Bericht
        {
            VanGebruikerId = userId!, NaarGebruikerId = u.CoachGebruikerId,
            Tekst = UitnodigingTekst(aanbod, u, maker.Naam, "heeft jouw aandeel aangepast en nodigt je opnieuw uit voor"),
            AangemaaktOp = DateTime.UtcNow, Gelezen = false
        });
        await _context.SaveChangesAsync();
        await HerberekenStatus(aanbod, userId!, maker.Naam);
        await _context.SaveChangesAsync();
        return Ok(new { u.Id, u.Status, u.Percentage });
    }

    [HttpDelete("{id:int}/coaches/{uitnodigingId:int}")]
    [Authorize(Roles = "Coach")]
    public async Task<IActionResult> CoachVerwijderen(int id, int uitnodigingId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var maker = await _userManager.FindByIdAsync(userId!);
        var aanbod = await _context.CoachAanboden.FindAsync(id);
        if (aanbod == null || maker == null) return NotFound();
        if (aanbod.CoachGebruikerId != userId) return Forbid();
        var u = await _context.CoachAanbodUitnodigingen.FirstOrDefaultAsync(x => x.Id == uitnodigingId && x.CoachAanbodId == id);
        if (u == null) return NotFound("Uitnodiging niet gevonden.");

        _context.CoachAanbodUitnodigingen.Remove(u);

        // Ook uit de groepschat van de training halen
        var groep = await _context.Groepsgesprekken.FirstOrDefaultAsync(g => g.CoachAanbodId == id);
        if (groep != null)
        {
            var lid = await _context.GroepsgesprekLeden
                .FirstOrDefaultAsync(l => l.GroepsgesprekId == groep.Id && l.GebruikerId == u.CoachGebruikerId);
            if (lid != null)
            {
                _context.GroepsgesprekLeden.Remove(lid);
                _context.Groepsberichten.Add(new Groepsbericht
                {
                    GroepsgesprekId = groep.Id, VanGebruikerId = userId!, VanNaam = maker.Naam,
                    Tekst = $"🚫 {u.CoachNaam} is geen coach meer bij deze training.",
                    AangemaaktOp = DateTime.UtcNow
                });
            }
        }

        _context.Berichten.Add(new Bericht
        {
            VanGebruikerId = userId!, NaarGebruikerId = u.CoachGebruikerId,
            Tekst = $"ℹ️ {maker.Naam} heeft je uitnodiging voor \"{aanbod.Titel}\" ingetrokken. Je hoeft niets meer te doen.",
            AangemaaktOp = DateTime.UtcNow, Gelezen = false
        });
        await _context.SaveChangesAsync();
        await HerberekenStatus(aanbod, userId!, maker.Naam);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Coach")]
    public async Task<IActionResult> Verwijderen(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var aanbod = await _context.CoachAanboden.FindAsync(id);
        if (aanbod == null) return NotFound();
        if (aanbod.CoachGebruikerId != userId) return Forbid();

        _context.CoachAanboden.Remove(aanbod);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}

public record CoachAanbodVerzoek(
    string Titel,
    string? KorteOmschrijving,
    string? Beschrijving,
    DateTime Datum,
    string Locatie,
    string? Categorieen,
    string? Disciplines,
    int? MaxRijders,
    decimal? PrijsPerRijder,
    bool IsGratis,
    bool IsGroep = true,
    List<UitgenodigdeCoachVerzoek>? UitgenodigdeCoaches = null
);

public record UitgenodigdeCoachVerzoek(string Id, decimal? Percentage);
public record PercentageVerzoek(decimal? Percentage);
