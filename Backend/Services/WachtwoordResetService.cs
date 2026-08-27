namespace Backend.Services;

public interface IWachtwoordResetService
{
    string MaakCode(string email);
    bool ValideerCode(string email, string code);
    void VerwijderCode(string email);
}

public class WachtwoordResetService : IWachtwoordResetService
{
    private readonly Dictionary<string, (string code, DateTime verloopt)> _codes = new();

    public string MaakCode(string email)
    {
        var code = Random.Shared.Next(100000, 999999).ToString();
        _codes[email.ToLower()] = (code, DateTime.UtcNow.AddMinutes(15));
        return code;
    }

    public bool ValideerCode(string email, string code)
    {
        var sleutel = email.ToLower();
        if (!_codes.TryGetValue(sleutel, out var entry)) return false;
        if (entry.verloopt < DateTime.UtcNow) { _codes.Remove(sleutel); return false; }
        return entry.code == code;
    }

    public void VerwijderCode(string email) => _codes.Remove(email.ToLower());
}
