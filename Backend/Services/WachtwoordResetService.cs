using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace Backend.Services;

public interface IWachtwoordResetService
{
    string MaakCode(string email);
    bool ValideerCode(string email, string code);
    void VerwijderCode(string email);
}

// 6-cijferige code, 15 minuten geldig. Na 5 foute pogingen vervalt de code (tegen raden).
public class WachtwoordResetService : IWachtwoordResetService
{
    private const int MaxPogingen = 5;
    private readonly ConcurrentDictionary<string, (string code, DateTime verloopt, int fout)> _codes = new();

    public string MaakCode(string email)
    {
        var code = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
        _codes[email.Trim().ToLowerInvariant()] = (code, DateTime.UtcNow.AddMinutes(15), 0);
        return code;
    }

    public bool ValideerCode(string email, string code)
    {
        var sleutel = (email ?? "").Trim().ToLowerInvariant();
        if (!_codes.TryGetValue(sleutel, out var entry)) return false;
        if (entry.verloopt < DateTime.UtcNow) { _codes.TryRemove(sleutel, out _); return false; }
        if (entry.code == (code ?? "").Trim()) return true;
        if (entry.fout + 1 >= MaxPogingen) _codes.TryRemove(sleutel, out _);
        else _codes[sleutel] = (entry.code, entry.verloopt, entry.fout + 1);
        return false;
    }

    public void VerwijderCode(string email) => _codes.TryRemove((email ?? "").Trim().ToLowerInvariant(), out _);
}
