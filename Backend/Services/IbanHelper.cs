using System.Numerics;
using System.Text;

namespace Backend.Services;

// Controle en opmaak van IBAN-rekeningnummers (ISO 13616, mod-97)
public static class IbanHelper
{
    // Geeft het IBAN zonder spaties in hoofdletters terug, of null als het geen geldig IBAN is
    public static string? Normaliseer(string? invoer)
    {
        if (string.IsNullOrWhiteSpace(invoer)) return null;
        var iban = new string(invoer.Where(ch => !char.IsWhiteSpace(ch) && ch != '-' && ch != '.').ToArray()).ToUpperInvariant();
        if (iban.Length < 15 || iban.Length > 34) return null;
        if (!char.IsLetter(iban[0]) || !char.IsLetter(iban[1]) || !char.IsDigit(iban[2]) || !char.IsDigit(iban[3])) return null;
        if (!iban.All(char.IsLetterOrDigit)) return null;
        if (iban.StartsWith("NL") && iban.Length != 18) return null;

        var herschikt = iban[4..] + iban[..4];
        var cijfers = new StringBuilder();
        foreach (var ch in herschikt)
            cijfers.Append(char.IsLetter(ch) ? (ch - 'A' + 10).ToString() : ch.ToString());
        return BigInteger.Parse(cijfers.ToString()) % 97 == 1 ? iban : null;
    }

    // NL91 ABNA 0417 1643 00
    public static string Opmaak(string? iban)
    {
        if (string.IsNullOrEmpty(iban)) return "";
        var sb = new StringBuilder();
        for (var i = 0; i < iban.Length; i++)
        {
            if (i > 0 && i % 4 == 0) sb.Append(' ');
            sb.Append(iban[i]);
        }
        return sb.ToString();
    }
}

// Hoe een coach betaald wordt: online via zijn eigen Mollie (als dat werkt), anders via overmaken
public record BetaalKeuze(string Wijze, string? Iban, string? Tenaamstelling)
{
    public const string Online = "online";
    public const string Overmaken = "overmaken";
    public bool IsOvermaken => Wijze == Overmaken;
}
