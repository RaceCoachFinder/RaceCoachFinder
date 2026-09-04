using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Backend.Models;

namespace Backend.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Coach> Coaches { get; set; }
    public DbSet<Rijder> Rijders { get; set; }
    public DbSet<Bericht> Berichten { get; set; }
    public DbSet<Boeking> Boekingen { get; set; }
    public DbSet<Review> Reviews { get; set; }
    public DbSet<CoachFavoriet> CoachFavorieten { get; set; }
    public DbSet<RijderAanvraag> RijderAanvragen { get; set; }
    public DbSet<RijderFavoriet> RijderFavorieten { get; set; }
    public DbSet<GebruikerInstellingen> GebruikerInstellingen { get; set; }
    public DbSet<AgendaItem> AgendaItems { get; set; }
}
