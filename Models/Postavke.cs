using System.ComponentModel.DataAnnotations.Schema;

namespace ThesisWebApp.Models;

[Table("postavke")]
public class Postavke
{
    [Column("kljuc")]
    public string Kljuc { get; set; } = string.Empty;

    [Column("vrednost")]
    public string Vrednost { get; set; } = string.Empty;
}
