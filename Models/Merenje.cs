using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ThesisWebApp.Models;

[Table("merenja")]
public class Merenje
{
    [Column("ime")]
    public string Ime { get; set; } = string.Empty;

    [Column("dt")]
    public DateTime Dt { get; set; }

    [Column("y")]
    public double? Y { get; set; }

    [Column("x")]
    public double? X { get; set; }

    [Column("z")]
    public double? Z { get; set; }

    [Column("dy")]
    public double? Dy { get; set; }

    [Column("dx")]
    public double? Dx { get; set; }

    [Column("dz")]
    public double? Dz { get; set; }

    [Column("d3d")]
    public double? D3d { get; set; }

    [Column("status")]
    public string Status { get; set; } = string.Empty;

    public LokacijaPredef? Lokacija { get; set; }
}

