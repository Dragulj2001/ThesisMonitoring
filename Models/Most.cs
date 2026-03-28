using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ThesisWebApp.Models;

public static class MostTip
{
    public const string DvaStuba = "Dva stuba";
    public const string JedanStub = "Jedan stub";

    public static bool JeValidan(string? tip)
    {
        var t = tip?.Trim();
        return t == DvaStuba || t == JedanStub;
    }
}

[Table("mostovi")]
public class Most
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("naziv")]
    public string Naziv { get; set; } = string.Empty;

    /// <summary>Vrednosti: <see cref="MostTip.DvaStuba"/>, <see cref="MostTip.JedanStub"/>.</summary>
    [Column("tip")]
    public string Tip { get; set; } = MostTip.DvaStuba;

    /// <summary>Granica alarma za d3d (iste jedinice kao koordinate, npr. m).</summary>
    [Column("limit_alarma")]
    public double LimitAlarma { get; set; } = 0.15;

    /// <summary>Granična vrednost |S_rel| za virtuelni senzor.</summary>
    [Column("limit_srel")]
    public double LimitSrel { get; set; } = 0.02;

    public ICollection<LokacijaPredef> Lokacije { get; set; } = new List<LokacijaPredef>();
}
