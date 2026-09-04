using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Backend.Data;
using Backend.Models;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InstellingenController : ControllerBase
{
    private readonly AppDbContext _context;

    public InstellingenController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var gebruikerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var inst = await _context.GebruikerInstellingen
            .FirstOrDefaultAsync(i => i.GebruikerId == gebruikerId);

        if (inst == null)
        {
            inst = new GebruikerInstellingen { GebruikerId = gebruikerId };
            _context.GebruikerInstellingen.Add(inst);
            await _context.SaveChangesAsync();
        }

        return Ok(inst);
    }

    [HttpPut]
    public async Task<IActionResult> Put([FromBody] GebruikerInstellingen update)
    {
        var gebruikerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var inst = await _context.GebruikerInstellingen
            .FirstOrDefaultAsync(i => i.GebruikerId == gebruikerId);

        if (inst == null)
        {
            inst = new GebruikerInstellingen { GebruikerId = gebruikerId };
            _context.GebruikerInstellingen.Add(inst);
        }

        inst.EmailAan                       = update.EmailAan;
        inst.BerichtenDrempel               = Math.Clamp(update.BerichtenDrempel, 1, 50);
        inst.AlleenFavorieten               = update.AlleenFavorieten;
        inst.EmailBijBetaalUpdate           = update.EmailBijBetaalUpdate;
        inst.EmailBijNieuweAanvraag         = update.EmailBijNieuweAanvraag;
        inst.EmailBijNieuweReview           = update.EmailBijNieuweReview;
        inst.ProfielOpenbaar                = update.ProfielOpenbaar;
        inst.BerichtenVanOnbekenden         = update.BerichtenVanOnbekenden;
        inst.AgendaReminderActief           = update.AgendaReminderActief;
        inst.AgendaReminderDagenVanTevoren  = Math.Clamp(update.AgendaReminderDagenVanTevoren, 1, 30);

        await _context.SaveChangesAsync();
        return Ok(inst);
    }
}
