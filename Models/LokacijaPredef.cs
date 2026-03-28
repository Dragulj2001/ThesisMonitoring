using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ThesisWebApp.Models;

[Table("lokacije_predef")]
public class LokacijaPredef
{
    [Key]
    [Column("ime")]
    public string Ime { get; set; } = string.Empty;

    [Column("y_koord")]
    public double? YKoord { get; set; }

    [Column("x_koord")]
    public double? XKoord { get; set; }

    [Column("z_koord")]
    public double? ZKoord { get; set; }

    [Column("most_id")]
    public int MostId { get; set; }

    /// <summary>Uloga u konstrukciji (npr. Vrh1, Sredina, Vrh — zavisi od tipa mosta).</summary>
    [Column("uloga")]
    public string Uloga { get; set; } = string.Empty;

    public Most? Most { get; set; }

    public ICollection<Merenje> Merenja { get; set; } = new List<Merenje>();
}

