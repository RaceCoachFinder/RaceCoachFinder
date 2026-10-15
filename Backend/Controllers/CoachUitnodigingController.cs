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
[Authorize]
public class CoachUitnodigingController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public CoachUitnodigingController(AppDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [HttpGet("mijn")]
    public async Task<IActionResult> GetMijn()
    {
        var mijnId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var lijst = await _context.CoachAanbodUitnodigingen
            .Where(u => u.CoachGebruikerId == mijnId && u.Status == "Openstaand")
            .OrderByDescending(u => u.AangemaaktOp)
            .ToListAsync();
        return Ok(lijst);
    }

    // Actuele status + alle informatie van een uitnodiging (voor de uitnodiging-kaart in berichten)
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetDetails(int id)
    {
        var mijnId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var u = await _context.CoachAanbodUitnodigingen.FindAsync(id);
        if (u == null) return NotFound("Deze uitnodiging is ingetrokken.");
        var aanbod = await _context.CoachAanboden.FindAsync(u.CoachAanbodId);
        if (aanbod == null) return NotFound("Deze training bestaat niet meer.");
        if (u.CoachGebruikerId != mijnId && aanbod.CoachGebruikerId != mijnId) return Forbid();

        var coaches = await _context.CoachAanbodUitnodigingen
            .Where(x => x.CoachAanbodId == aanbod.Id)
            .Select(x => new { x.CoachNaam, x.Status, x.Percentage })
            .ToListAsync();

        return Ok(new
        {
            u.Id, u.Status, u.Percentage,
            Training = new
            {
                aanbod.Id, aanbod.Titel, aanbod.KorteOmschrijving, aanbod.Beschrijving, aanbod.Datum,
                aanbod.Locatie, aanbod.Categorieen, aanbod.Disciplines, aanbod.MaxRijders,
                aanbod.PrijsPerRijder, aanbod.IsGratis, aanbod.WachtOpCoaches,
                MakerNaam = aanbod.CoachNaam
            },
            Coaches = coaches
        });
    }

    [HttpPut("{id}/accepteer")]
    public async Task<IActionResult> Accepteer(int id)
    {
        var mijnId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var uitnodiging = await _context.CoachAanbodUitnodigingen.FindAsync(id);
        if (uitnodiging == null || uitnodiging.CoachGebruikerId != mijnId)
            return NotFound();
        if (uitnodiging.Status != "Openstaand")
            return BadRequest("Uitnodiging is al verwerkt.");

        uitnodiging.Status = "Geaccepteerd";

        var groep = await _context.Groepsgesprekken
            .FirstOrDefaultAsync(g => g.CoachAanbodId == uitnodiging.CoachAanbodId);

        if (groep != null)
        {
            var isAlLid = await _context.GroepsgesprekLeden
                .AnyAsync(l => l.GroepsgesprekId == groep.Id && l.GebruikerId == mijnId);

            if (!isAlLid)
            {
                _context.GroepsgesprekLeden.Add(new GroepsgesprekLid
                {
                    GroepsgesprekId = groep.Id,
                    GebruikerId = mijnId!,
                    GebruikerNaam = uitnodiging.CoachNaam,
                    ToegetreedOp = DateTime.UtcNow
                });
                _context.Groepsberichten.Add(new Groepsbericht
                {
                    GroepsgesprekId = groep.Id,
                    VanGebruikerId = mijnId!,
                    VanNaam = uitnodiging.CoachNaam,
                    Tekst = $"✅ {uitnodiging.CoachNaam} heeft de uitnodiging geaccepteerd.",
                    AangemaaktOp = DateTime.UtcNow
                });
            }
        }

        // Controleer of alle uitnodigingen voor dit aanbod geaccepteerd zijn
        var aanbod = await _context.CoachAanboden.FindAsync(uitnodiging.CoachAanbodId);
        if (aanbod != null)
        {
            // Let op: deze uitnodiging is nog niet opgeslagen als 'Geaccepteerd', dus die niet meetellen
            var nogOpenstaand = await _context.CoachAanbodUitnodigingen
                .AnyAsync(u => u.CoachAanbodId == uitnodiging.CoachAanbodId && u.Id != uitnodiging.Id && u.Status != "Geaccepteerd");
            if (!nogOpenstaand)
            {
                aanbod.IsActief = true;
                aanbod.WachtOpCoaches = false;
                if (groep != null)
                {
                    _context.Groepsberichten.Add(new Groepsbericht
                    {
                        GroepsgesprekId = groep.Id,
                        VanGebruikerId = mijnId!,
                        VanNaam = uitnodiging.CoachNaam,
                        Tekst = "🎉 Alle coaches hebben geaccepteerd. De training staat nu online!",
                        AangemaaktOp = DateTime.UtcNow
                    });
                }
            }
        }

        await _context.SaveChangesAsync();
        return Ok(new { groepsgesprekId = groep?.Id, groepsNaam = groep?.Naam, uitnodiging.Percentage });
    }

    [HttpPut("{id}/wijs-af")]
    public async Task<IActionResult> WijsAf(int id)
    {
        var mijnId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var uitnodiging = await _context.CoachAanbodUitnodigingen.FindAsync(id);
        if (uitnodiging == null || uitnodiging.CoachGebruikerId != mijnId)
            return NotFound();
        if (uitnodiging.Status != "Openstaand")
            return BadRequest("Uitnodiging is al verwerkt.");

        uitnodiging.Status = "Afgewezen";

        // Stuur bericht naar de maker van de training
        var aanbod = await _context.CoachAanboden.FindAsync(uitnodiging.CoachAanbodId);
        if (aanbod != null)
        {
            _context.Berichten.Add(new Bericht
            {
                VanGebruikerId = mijnId!,
                NaarGebruikerId = aanbod.CoachGebruikerId,
                Tekst = $"❌ {uitnodiging.CoachNaam} heeft de uitnodiging voor \"{uitnodiging.AanbodTitel}\" geweigerd. Je kan de training aanpassen zodat hij alsnog online kan komen.",
                AangemaaktOp = DateTime.UtcNow,
                Gelezen = false
            });

            // Bericht in groepschat
            var groep = await _context.Groepsgesprekken
                .FirstOrDefaultAsync(g => g.CoachAanbodId == uitnodiging.CoachAanbodId);
            if (groep != null)
            {
                _context.Groepsberichten.Add(new Groepsbericht
                {
                    GroepsgesprekId = groep.Id,
                    VanGebruikerId = mijnId!,
                    VanNaam = uitnodiging.CoachNaam,
                    Tekst = $"❌ {uitnodiging.CoachNaam} heeft de uitnodiging geweigerd.",
                    AangemaaktOp = DateTime.UtcNow
                });
            }
        }

        await _context.SaveChangesAsync();
        return Ok();
    }
}
