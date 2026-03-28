namespace ThesisWebApp.Models;

/// <summary>Dozvoljene uloge prizme u zavisnosti od tipa mosta.</summary>
public static class PrizmaUloga
{
    public const string Vrh1 = "Vrh1";
    public const string Dno1 = "Dno1";
    public const string Sredina = "Sredina";
    public const string Vrh2 = "Vrh2";
    public const string Dno2 = "Dno2";

    public const string Vrh = "Vrh";
    public const string Dno = "Dno";

    public static readonly string[] ZaDvaStuba = [Vrh1, Dno1, Sredina, Vrh2, Dno2];
    public static readonly string[] ZaJedanStub = [Vrh, Sredina, Dno];

    public static bool JeValidnaZaTip(string? tipMosta, string? uloga)
    {
        var u = uloga?.Trim();
        if (string.IsNullOrEmpty(u))
            return false;
        if (string.Equals(tipMosta?.Trim(), MostTip.JedanStub, StringComparison.Ordinal))
            return ZaJedanStub.Contains(u, StringComparer.Ordinal);
        return ZaDvaStuba.Contains(u, StringComparer.Ordinal);
    }
}
