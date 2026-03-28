namespace ThesisWebApp.Models;

public class MostOpcija
{
    public int Id { get; set; }
    public string Naziv { get; set; } = string.Empty;
    public string Tip { get; set; } = MostTip.DvaStuba;
}

public class AnalizaViewModel
{
    public int? SelectedMostId { get; set; }
    public List<MostOpcija> Mostovi { get; set; } = new();

    public string? SelectedIme { get; set; }
    /// <summary>Uloga trenutno izabrane prizme (prikaz u kartici fiksnih koordinata).</summary>
    public string? SelectedUloga { get; set; }
    public List<string> Prizme { get; set; } = new();

    public double? YKoord { get; set; }
    public double? XKoord { get; set; }
    public double? ZKoord { get; set; }

    public List<MerenjeHistoryRow> Measurements { get; set; } = new();
    public double AlarmLimit { get; set; } = 0.15;
    /// <summary>Granična vrednost |S_rel| za virtuelni senzor (npr. 0.02).</summary>
    public double LimitSrel { get; set; } = 0.02;
}

public class MerenjeHistoryRow
{
    public DateTime Dt { get; set; }
    public double? Y { get; set; }
    public double? X { get; set; }
    public double? Z { get; set; }
    public double? Dy { get; set; }
    public double? Dx { get; set; }
    public double? Dz { get; set; }
    public double? D3d { get; set; }
    public string Status { get; set; } = string.Empty;
}

