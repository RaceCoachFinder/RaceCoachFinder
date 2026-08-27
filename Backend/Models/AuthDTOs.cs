namespace Backend.Models;

public record RegistrerenVerzoek(string Naam, string Email, string Wachtwoord, string Rol);
public record InloggenVerzoek(string Email, string Wachtwoord);
public record InloggenAntwoord(string Token, string Id, string Naam, string Email, string Rol, bool HeeftAccountIngericht);
public record AccountInrichtenVerzoek(string NieuwEmail, string NieuwWachtwoord);
