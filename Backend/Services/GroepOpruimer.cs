using Microsoft.EntityFrameworkCore;
using Backend.Data;

namespace Backend.Services;

// Een groepsgesprek waar niemand meer in zit, wordt met alles erbij uit de database gehaald
public static class GroepOpruimer
{
    // Roep aan NA het opslaan van het vertrek/verwijderen van een lid
    public static async Task VerwijderLegeGroepenAsync(AppDbContext context, IEnumerable<int> groepIds)
    {
        foreach (var groepId in groepIds.Distinct().ToList())
        {
            if (await context.GroepsgesprekLeden.AnyAsync(l => l.GroepsgesprekId == groepId)) continue;
            var groep = await context.Groepsgesprekken.FindAsync(groepId);
            if (groep == null) continue;
            context.Groepsberichten.RemoveRange(context.Groepsberichten.Where(b => b.GroepsgesprekId == groepId));
            context.GroepsgesprekVerwijderingen.RemoveRange(context.GroepsgesprekVerwijderingen.Where(v => v.GroepsgesprekId == groepId));
            context.TrainingVoorstellen.RemoveRange(context.TrainingVoorstellen.Where(v => v.GroepsgesprekId == groepId));
            context.Groepsgesprekken.Remove(groep);
        }
        await context.SaveChangesAsync();
    }
}
