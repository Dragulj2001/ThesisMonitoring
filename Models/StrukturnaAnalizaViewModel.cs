namespace ThesisWebApp.Models;

public class StrukturnaAnalizaViewModel
{
    public List<MostOpcija> Mostovi { get; set; } = new();
    public int? SelectedMostId { get; set; }
    /// <summary>Tip tekst iz baze (npr. <see cref="MostTip.DvaStuba"/>).</summary>
    public string TipMosta { get; set; } = MostTip.DvaStuba;

    public bool JedanStubMost => string.Equals(TipMosta?.Trim(), MostTip.JedanStub, StringComparison.Ordinal);

    /// <summary>Kratki opis formule za kartu (HTML-safe tekst u view-u).</summary>
    public string SrelFormulaOpis { get; set; } = string.Empty;

    public List<StrukturnaMapMarker> Markers { get; set; } = new();
    public List<StrukturnaSrelPoint> SrelSeries { get; set; } = new();
    /// <summary>Crveni baner: prekoračenje i (nije potvrđeno ili ima noviji prekršaj posle potvrde).</summary>
    public bool SrelStructuralAlarm { get; set; }
    /// <summary>Maks. S_rel u prikazanom nizu, u mm (koordinate u m).</summary>
    public double? MaxSrelSeriesMm { get; set; }
    public bool ShowAcknowledgeSrelAlarm { get; set; }
}

public class StrukturnaMapMarker
{
    public string Ime { get; set; } = string.Empty;
    public string Uloga { get; set; } = string.Empty;
    /// <summary>Koordinate u SVG viewBox-u (0 0 800 320).</summary>
    public double SvgCx { get; set; }
    public double SvgCy { get; set; }
    public double? LatestD3d { get; set; }
    /// <summary>OK, WARN, ALARM, ili NONE ako nema merenja / d3d.</summary>
    public string StatusTier { get; set; } = "NONE";
}

public class StrukturnaSrelPoint
{
    public DateTime Dt { get; set; }
    public double Srel { get; set; }
}
