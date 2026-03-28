namespace ThesisWebApp.Models;

public class MerenjeViewModel
{
    public int MostId { get; set; }

    public string NazivMosta { get; set; } = string.Empty;

    public string Ime { get; set; } = string.Empty;
    public DateTime Dt { get; set; }

    public double? Y { get; set; }
    public double? X { get; set; }
    public double? Z { get; set; }

    public double? Dy { get; set; }
    public double? Dx { get; set; }
    public double? Dz { get; set; }
    public double? D3d { get; set; }

    public string Status { get; set; } = string.Empty;

    public double? YKoord { get; set; }
    public double? XKoord { get; set; }
    public double? ZKoord { get; set; }

    /// <summary>Granica d3d za most ove prizme (prikaz statusa u tabeli).</summary>
    public double LimitAlarma { get; set; } = 0.15;
}

