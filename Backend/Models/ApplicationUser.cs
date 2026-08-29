using Microsoft.AspNetCore.Identity;

namespace Backend.Models;

public class ApplicationUser : IdentityUser
{
    public string Naam { get; set; } = string.Empty;
    public string Rol { get; set; } = "Rijder"; // Coach, Rijder, Admin
    public bool HeeftAccountIngericht { get; set; } = true; // false = code-account, moet nog email+wachtwoord instellen
    public DateTime? GratisVerlooptOp { get; set; }
    public string? MollieKlantId { get; set; }
    public bool AbonnementActief { get; set; } = false;
    public DateTime? AbonnementVerlooptOp { get; set; }
}
