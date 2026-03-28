using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using ThesisWebApp.Data;
using ThesisWebApp.Models;
using ThesisWebApp.Services;
using ClosedXML.Excel;

namespace ThesisWebApp.Controllers;

[Authorize]
[AutoValidateAntiforgeryToken]
public class DataController : Controller
{
    private const string SrelAlarmAckUtcKey = "SrelAlarmAckUtc";
    private const double DefaultLimitAlarma = 0.15;
    private const double DefaultLimitSrel = 0.02;

    private static string SrelAlarmAckUtcKeyForMost(int mostId) => $"{SrelAlarmAckUtcKey}:Most:{mostId}";
    private readonly ApplicationDbContext _context;
    private readonly IEmailService _emailService;

    public DataController(ApplicationDbContext context, IEmailService emailService)
    {
        _context = context;
        _emailService = emailService;
    }

    private static double EffectiveLimitAlarma(double stored) =>
        stored > 0 ? stored : DefaultLimitAlarma;

    private static double EffectiveLimitSrel(double stored) =>
        stored > 0 ? stored : DefaultLimitSrel;

    public async Task<IActionResult> Index()
    {
        var query = from m in _context.Merenja
                    join l in _context.LokacijePredef on m.Ime equals l.Ime
                    join mo in _context.Mostovi on l.MostId equals mo.Id
                    select new MerenjeViewModel
                    {
                        MostId = l.MostId,
                        NazivMosta = mo.Naziv,
                        Ime = m.Ime,
                        Dt = m.Dt,
                        Y = m.Y,
                        X = m.X,
                        Z = m.Z,
                        Dy = m.Dy,
                        Dx = m.Dx,
                        Dz = m.Dz,
                        D3d = m.D3d,
                        Status = m.Status,
                        YKoord = l.YKoord,
                        XKoord = l.XKoord,
                        ZKoord = l.ZKoord,
                        LimitAlarma = EffectiveLimitAlarma(mo.LimitAlarma)
                    };

        var model = await query.AsNoTracking().OrderByDescending(m => m.Dt).ToListAsync();

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Analiza(int? mostId, string? prizmaIme, string? ime)
    {
        var prizmaParam = string.IsNullOrWhiteSpace(prizmaIme) ? ime : prizmaIme;

        var mostovi = await _context.Mostovi
            .AsNoTracking()
            .OrderBy(m => m.Id)
            .Select(m => new MostOpcija { Id = m.Id, Naziv = m.Naziv, Tip = m.Tip })
            .ToListAsync();

        var model = new AnalizaViewModel
        {
            Mostovi = mostovi,
            AlarmLimit = DefaultLimitAlarma,
            LimitSrel = DefaultLimitSrel
        };

        if (mostovi.Count == 0)
        {
            ViewBag.Limit = DefaultLimitAlarma;
            return View(model);
        }

        int selectedMostId;
        if (mostId.HasValue && mostovi.Exists(m => m.Id == mostId.Value))
            selectedMostId = mostId.Value;
        else if (!string.IsNullOrWhiteSpace(prizmaParam))
        {
            var locMost = await _context.LokacijePredef.AsNoTracking()
                .Where(l => l.Ime == prizmaParam.Trim())
                .Select(l => (int?)l.MostId)
                .FirstOrDefaultAsync();
            selectedMostId = locMost ?? mostovi[0].Id;
        }
        else
            selectedMostId = mostovi[0].Id;

        var mostZaLimite = await _context.Mostovi.AsNoTracking()
            .FirstAsync(m => m.Id == selectedMostId);
        var alarmLimit = EffectiveLimitAlarma(mostZaLimite.LimitAlarma);
        var limitSrel = EffectiveLimitSrel(mostZaLimite.LimitSrel);
        model.AlarmLimit = alarmLimit;
        model.LimitSrel = limitSrel;

        var prizme = await _context.LokacijePredef
            .AsNoTracking()
            .Where(l => l.MostId == selectedMostId)
            .Select(l => l.Ime)
            .Distinct()
            .ToListAsync();

        prizme = prizme
            .OrderBy(p => GetLeadingNumber(p))
            .ThenBy(p => p)
            .ToList();

        string? resolvedPrizma = string.IsNullOrWhiteSpace(prizmaParam) ? null : prizmaParam.Trim();
        if (resolvedPrizma == null || !prizme.Contains(resolvedPrizma))
            resolvedPrizma = prizme.Count > 0 ? prizme[0] : null;

        model.SelectedMostId = selectedMostId;
        model.Prizme = prizme;
        model.SelectedIme = resolvedPrizma;

        LokacijaPredef? lokacijaZaPrikaz = null;
        if (!string.IsNullOrWhiteSpace(resolvedPrizma))
        {
            lokacijaZaPrikaz = await _context.LokacijePredef
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.Ime == resolvedPrizma);
            if (lokacijaZaPrikaz == null || lokacijaZaPrikaz.MostId != selectedMostId)
            {
                resolvedPrizma = prizme.Count > 0 ? prizme[0] : null;
                model.SelectedIme = resolvedPrizma;
                lokacijaZaPrikaz = !string.IsNullOrWhiteSpace(resolvedPrizma)
                    ? await _context.LokacijePredef.AsNoTracking().FirstOrDefaultAsync(l => l.Ime == resolvedPrizma)
                    : null;
            }
        }

        if (!string.IsNullOrWhiteSpace(resolvedPrizma) && lokacijaZaPrikaz != null)
        {
            model.YKoord = lokacijaZaPrikaz.YKoord;
            model.XKoord = lokacijaZaPrikaz.XKoord;
            model.ZKoord = lokacijaZaPrikaz.ZKoord;
            model.SelectedUloga = string.IsNullOrWhiteSpace(lokacijaZaPrikaz.Uloga) ? null : lokacijaZaPrikaz.Uloga;

            var merenja = await _context.Merenja
                .AsNoTracking()
                .Where(m => m.Ime == resolvedPrizma)
                .OrderByDescending(m => m.Dt)
                .ToListAsync();

            model.Measurements = merenja.Select(m => new MerenjeHistoryRow
            {
                Dt = m.Dt,
                Y = m.Y,
                X = m.X,
                Z = m.Z,
                Dy = m.Dy,
                Dx = m.Dx,
                Dz = m.Dz,
                D3d = m.D3d,
                Status = string.Empty
            }).ToList();
        }

        ViewBag.Limit = alarmLimit;
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> StrukturnaAnaliza(int? mostId)
    {
        var mostovi = await _context.Mostovi
            .AsNoTracking()
            .OrderBy(m => m.Id)
            .Select(m => new MostOpcija { Id = m.Id, Naziv = m.Naziv, Tip = m.Tip })
            .ToListAsync();

        var model = new StrukturnaAnalizaViewModel
        {
            Mostovi = mostovi,
            SrelFormulaOpis = string.Empty
        };

        if (mostovi.Count == 0)
        {
            ViewBag.Limit = DefaultLimitAlarma;
            ViewBag.LimitSrel = DefaultLimitSrel;
            return View(model);
        }

        var selectedMostId = mostId is { } mid && mostovi.Exists(m => m.Id == mid)
            ? mid
            : mostovi[0].Id;
        var mostRow = mostovi.First(m => m.Id == selectedMostId);
        var jedanStub = string.Equals(mostRow.Tip, MostTip.JedanStub, StringComparison.Ordinal);

        var mostLimits = await _context.Mostovi.AsNoTracking()
            .Where(m => m.Id == selectedMostId)
            .Select(m => new { m.LimitAlarma, m.LimitSrel })
            .FirstAsync();
        var alarmLimit = EffectiveLimitAlarma(mostLimits.LimitAlarma);
        var limitSrel = EffectiveLimitSrel(mostLimits.LimitSrel);
        ViewBag.Limit = alarmLimit;
        ViewBag.LimitSrel = limitSrel;

        model.SelectedMostId = selectedMostId;
        model.TipMosta = mostRow.Tip;
        model.SrelFormulaOpis = jedanStub
            ? "S_rel = dZ_Sredina − dZ_Vrh (jedan stub — referenca jednog vrha pylona)."
            : "S_rel = dZ_Sredina − (dZ_Vrh1 + dZ_Vrh2) / 2 (dva stuba — referenca oba vrha).";

        var lokacije = await _context.LokacijePredef.AsNoTracking()
            .Where(l => l.MostId == selectedMostId && l.Uloga != null && l.Uloga != "")
            .Select(l => new { l.Ime, Uloga = l.Uloga!.Trim() })
            .ToListAsync();

        var imenaNaMostu = lokacije.Select(l => l.Ime).Distinct(StringComparer.Ordinal).ToList();
        var imeToUloga = lokacije
            .GroupBy(l => l.Ime, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.First().Uloga, StringComparer.Ordinal);

        var ulogaToIme = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var l in lokacije.OrderBy(l => l.Ime, StringComparer.Ordinal))
        {
            if (!PrizmaUloga.JeValidnaZaTip(mostRow.Tip, l.Uloga))
                continue;
            if (!ulogaToIme.ContainsKey(l.Uloga))
                ulogaToIme[l.Uloga] = l.Ime;
        }

        var latestByIme = new Dictionary<string, Merenje>(StringComparer.Ordinal);
        foreach (var ime in imenaNaMostu)
        {
            var m = await _context.Merenja.AsNoTracking()
                .Where(x => x.Ime == ime)
                .OrderByDescending(x => x.Dt)
                .FirstOrDefaultAsync();
            if (m != null)
                latestByIme[ime] = m;
        }

        var markers = new List<StrukturnaMapMarker>();
        foreach (var kv in ulogaToIme.OrderBy(k => k.Key, StringComparer.Ordinal))
        {
            var uloga = kv.Key;
            var ime = kv.Value;
            var (cx, cy) = GetStrukturnaMarkerCoordsForUloga(uloga, jedanStub);
            latestByIme.TryGetValue(ime, out var last);
            markers.Add(new StrukturnaMapMarker
            {
                Ime = ime,
                Uloga = uloga,
                SvgCx = cx,
                SvgCy = cy,
                LatestD3d = last?.D3d,
                StatusTier = GetStrukturnaMarkerTier(last, alarmLimit)
            });
        }

        var rows = imenaNaMostu.Count == 0
            ? new List<Merenje>()
            : await _context.Merenja.AsNoTracking()
                .Where(m => imenaNaMostu.Contains(m.Ime))
                .OrderBy(m => m.Dt)
                .ThenBy(m => m.Ime)
                .ToListAsync();

        var srelSeries = BuildSrelSeries(rows, imeToUloga, jedanStub);
        var exceedsLimit = srelSeries.Count > 0 && srelSeries.Exists(p => p.Srel > limitSrel);
        var maxSrelMm = srelSeries.Count > 0
            ? srelSeries.Max(p => p.Srel) * 1000.0
            : (double?)null;

        DateTime? ackUtc = null;
        try
        {
            var ackKey = SrelAlarmAckUtcKeyForMost(selectedMostId);
            var ackRow = await _context.Postavke.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Kljuc == ackKey);
            ackUtc = ParseSrelAlarmAckUtc(ackRow?.Vrednost);
        }
        catch { }

        var bannerAlarm = exceedsLimit && (
            !ackUtc.HasValue
            || srelSeries.Any(p =>
                p.Srel > limitSrel && p.Dt.ToUniversalTime() > ackUtc.Value));

        model.Markers = markers;
        model.SrelSeries = srelSeries;
        model.SrelStructuralAlarm = bannerAlarm;
        model.MaxSrelSeriesMm = maxSrelMm;
        model.ShowAcknowledgeSrelAlarm = bannerAlarm && User.IsInRole("Admin");

        return View(model);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> AcknowledgeSrelAlarm([FromForm] int? mostId)
    {
        if (!mostId.HasValue)
            return RedirectToAction(nameof(StrukturnaAnaliza));

        var val = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);
        var ackKey = SrelAlarmAckUtcKeyForMost(mostId.Value);
        var row = await _context.Postavke.FirstOrDefaultAsync(x => x.Kljuc == ackKey);
        if (row != null)
            row.Vrednost = val;
        else
            _context.Postavke.Add(new Postavke { Kljuc = ackKey, Vrednost = val });
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(StrukturnaAnaliza), new { mostId = mostId.Value });
    }

    private static DateTime? ParseSrelAlarmAckUtc(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;
        if (!DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dt))
            return null;
        return dt.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(dt, DateTimeKind.Utc)
            : dt.ToUniversalTime();
    }

    private static string GetStrukturnaMarkerTier(Merenje? last, double alarmLimit)
    {
        if (last?.D3d is not { } d3d)
            return "NONE";
        return ClassifyD3dStatus(d3d, alarmLimit);
    }

    private static List<StrukturnaSrelPoint> BuildSrelSeries(
        List<Merenje> rowsOrdered,
        Dictionary<string, string> imeToUloga,
        bool jedanStub)
    {
        Dictionary<string, double?> trackers = jedanStub
            ? new Dictionary<string, double?>(StringComparer.Ordinal)
            {
                [PrizmaUloga.Vrh] = null,
                [PrizmaUloga.Sredina] = null
            }
            : new Dictionary<string, double?>(StringComparer.Ordinal)
            {
                [PrizmaUloga.Vrh1] = null,
                [PrizmaUloga.Vrh2] = null,
                [PrizmaUloga.Sredina] = null
            };

        var points = new List<StrukturnaSrelPoint>();
        foreach (var row in rowsOrdered)
        {
            if (!imeToUloga.TryGetValue(row.Ime, out var uRaw))
                continue;
            var u = uRaw.Trim();
            if (!trackers.ContainsKey(u))
                continue;

            trackers[u] = row.Dz;

            double? srel = null;
            if (jedanStub)
            {
                if (trackers[PrizmaUloga.Sredina] is { } zs && trackers[PrizmaUloga.Vrh] is { } zv)
                    srel = zs - zv;
            }
            else
            {
                if (trackers[PrizmaUloga.Sredina] is { } zmid && trackers[PrizmaUloga.Vrh1] is { } zl && trackers[PrizmaUloga.Vrh2] is { } zr)
                    srel = zmid - (zl + zr) / 2;
            }

            if (srel.HasValue)
                points.Add(new StrukturnaSrelPoint { Dt = row.Dt, Srel = srel.Value });
        }

        return points;
    }

    private async Task<bool> ParticipatesInVirtualSensorAsync(string? ime, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(ime))
            return false;
        var key = ime.Trim();
        var row = await (from l in _context.LokacijePredef.AsNoTracking()
                join mo in _context.Mostovi.AsNoTracking() on l.MostId equals mo.Id
                where l.Ime == key
                select new { l.Uloga, mo.Tip })
            .FirstOrDefaultAsync(cancellationToken);
        if (row is null || string.IsNullOrWhiteSpace(row.Uloga))
            return false;
        if (!PrizmaUloga.JeValidnaZaTip(row.Tip, row.Uloga))
            return false;
        var u = row.Uloga.Trim();
        if (string.Equals(row.Tip, MostTip.JedanStub, StringComparison.Ordinal))
            return u == PrizmaUloga.Sredina || u == PrizmaUloga.Vrh;
        return u == PrizmaUloga.Sredina || u == PrizmaUloga.Vrh1 || u == PrizmaUloga.Vrh2;
    }

    private async Task<double> GetLimitSrelForMostAsync(int mostId, CancellationToken cancellationToken = default)
    {
        var v = await _context.Mostovi.AsNoTracking()
            .Where(m => m.Id == mostId)
            .Select(m => m.LimitSrel)
            .FirstOrDefaultAsync(cancellationToken);
        return EffectiveLimitSrel(v);
    }

    private async Task<double?> GetLatestDzAsync(string ime, CancellationToken cancellationToken = default)
    {
        var m = await _context.Merenja.AsNoTracking()
            .Where(x => x.Ime == ime)
            .OrderByDescending(x => x.Dt)
            .FirstOrDefaultAsync(cancellationToken);
        return m?.Dz;
    }

    /// <summary>Najnoviji S_rel za dati most, prema ulogama prizmi (dva stuba ili jedan stub).</summary>
    private async Task<double?> ComputeLatestSrelForMostAsync(int mostId, CancellationToken cancellationToken = default)
    {
        var tip = await _context.Mostovi.AsNoTracking()
            .Where(m => m.Id == mostId)
            .Select(m => m.Tip)
            .FirstOrDefaultAsync(cancellationToken);
        if (string.IsNullOrEmpty(tip))
            return null;

        var jedan = string.Equals(tip, MostTip.JedanStub, StringComparison.Ordinal);

        var lokacije = await _context.LokacijePredef.AsNoTracking()
            .Where(l => l.MostId == mostId && l.Uloga != null && l.Uloga != "")
            .Select(l => new { l.Ime, Uloga = l.Uloga!.Trim() })
            .ToListAsync(cancellationToken);

        string? imeSred = null, imeVrh1 = null, imeVrh2 = null, imeVrh = null;
        foreach (var l in lokacije)
        {
            if (!PrizmaUloga.JeValidnaZaTip(tip, l.Uloga))
                continue;
            if (l.Uloga == PrizmaUloga.Sredina)
                imeSred ??= l.Ime;
            if (jedan)
            {
                if (l.Uloga == PrizmaUloga.Vrh)
                    imeVrh ??= l.Ime;
            }
            else
            {
                if (l.Uloga == PrizmaUloga.Vrh1)
                    imeVrh1 ??= l.Ime;
                if (l.Uloga == PrizmaUloga.Vrh2)
                    imeVrh2 ??= l.Ime;
            }
        }

        if (jedan)
        {
            if (imeSred is null || imeVrh is null)
                return null;
            var zs = await GetLatestDzAsync(imeSred, cancellationToken);
            var zv = await GetLatestDzAsync(imeVrh, cancellationToken);
            if (zs is not { } a || zv is not { } b)
                return null;
            return a - b;
        }

        if (imeSred is null || imeVrh1 is null || imeVrh2 is null)
            return null;
        var zMid = await GetLatestDzAsync(imeSred, cancellationToken);
        var zL = await GetLatestDzAsync(imeVrh1, cancellationToken);
        var zR = await GetLatestDzAsync(imeVrh2, cancellationToken);
        if (zMid is not { } m || zL is not { } lft || zR is not { } rgt)
            return null;
        return m - (lft + rgt) / 2;
    }

    private async Task TryNotifyVirtualSensorSrelCriticalAsync(int mostId, CancellationToken cancellationToken = default)
    {
        var srel = await ComputeLatestSrelForMostAsync(mostId, cancellationToken);
        if (srel is null)
            return;
        var mostRow = await _context.Mostovi.AsNoTracking()
            .Where(m => m.Id == mostId)
            .Select(m => new { m.Naziv, m.LimitSrel, m.LimitAlarma })
            .FirstOrDefaultAsync(cancellationToken);
        if (mostRow is null)
            return;
        var limitSrel = EffectiveLimitSrel(mostRow.LimitSrel);
        var limitAlarma = EffectiveLimitAlarma(mostRow.LimitAlarma);
        if (srel.Value <= limitSrel)
            return;
        try
        {
            await _emailService.SendSrelCriticalEmailAsync(
                mostRow.Naziv,
                srel.Value * 1000.0,
                limitSrel * 1000.0,
                limitAlarma * 1000.0,
                cancellationToken);
        }
        catch
        {
            // Ne blokiraj čuvanje ako SMTP ne uspe
        }
    }

    /// <summary>Pozicije markera u SVG viewBox 800×320 prema ulozi.</summary>
    private static (double Cx, double Cy) GetStrukturnaMarkerCoordsForUloga(string uloga, bool jedanStub)
    {
        var u = uloga?.Trim() ?? "";
        if (jedanStub)
        {
            return u switch
            {
                PrizmaUloga.Vrh => (400, 52),
                PrizmaUloga.Dno => (400, 272),
                PrizmaUloga.Sredina => (560, 202),
                _ => (400, 202)
            };
        }

        return u switch
        {
            PrizmaUloga.Dno1 => (158, 272),
            PrizmaUloga.Vrh1 => (158, 52),
            PrizmaUloga.Sredina => (400, 202),
            PrizmaUloga.Dno2 => (642, 272),
            PrizmaUloga.Vrh2 => (642, 52),
            _ => (400, 202)
        };
    }

    private static string ClassifyD3dStatus(double d3d, double alarmLimit)
    {
        if (d3d > alarmLimit)
            return "ALARM";
        if (d3d >= alarmLimit * 0.7 && d3d <= alarmLimit)
            return "WARN";
        return "OK";
    }

    private static void ApplyRecalculatedDisplacement(Merenje m, double refX, double refY, double refZ, double alarmLimit)
    {
        var mx = m.X ?? 0;
        var my = m.Y ?? 0;
        var mz = m.Z ?? 0;
        var dx = mx - refX;
        var dy = my - refY;
        var dz = mz - refZ;
        var d3d = Math.Sqrt(dx * dx + dy * dy + dz * dz);
        m.Dx = dx;
        m.Dy = dy;
        m.Dz = dz;
        m.D3d = d3d;
        m.Status = ClassifyD3dStatus(d3d, alarmLimit);
    }

    private static int GetLeadingNumber(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return int.MaxValue;

        var s = value.Trim();
        var i = 0;
        while (i < s.Length && char.IsDigit(s[i])) i++;

        if (i == 0)
            return int.MaxValue;

        return int.TryParse(s.Substring(0, i), out var n) ? n : int.MaxValue;
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> CreateBridge([FromForm] string naziv, [FromForm] string tip)
    {
        if (string.IsNullOrWhiteSpace(naziv))
        {
            TempData["ImportMessage"] = "Naziv mosta je obavezan.";
            TempData["ImportSuccess"] = false;
            return RedirectToAction(nameof(Analiza));
        }

        if (!MostTip.JeValidan(tip))
        {
            TempData["ImportMessage"] = "Tip mosta mora biti „Dva stuba” ili „Jedan stub”.";
            TempData["ImportSuccess"] = false;
            return RedirectToAction(nameof(Analiza));
        }

        var m = new Most
        {
            Naziv = naziv.Trim(),
            Tip = tip.Trim(),
            LimitAlarma = DefaultLimitAlarma,
            LimitSrel = DefaultLimitSrel
        };
        _context.Mostovi.Add(m);
        await _context.SaveChangesAsync();
        TempData["ImportSuccess"] = true;
        TempData["ImportMessage"] = $"Most „{m.Naziv}” je dodat.";
        return RedirectToAction(nameof(Analiza), new { mostId = m.Id });
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> DeleteBridge([FromForm] int mostId)
    {
        var most = await _context.Mostovi.FirstOrDefaultAsync(m => m.Id == mostId);
        if (most == null)
            return NotFound("Most nije pronađen.");

        var imena = await _context.LokacijePredef.Where(l => l.MostId == mostId).Select(l => l.Ime).ToListAsync();
        if (imena.Count > 0)
            await _context.Merenja.Where(m => imena.Contains(m.Ime)).ExecuteDeleteAsync();

        await _context.LokacijePredef.Where(l => l.MostId == mostId).ExecuteDeleteAsync();

        _context.Mostovi.Remove(most);
        await _context.SaveChangesAsync();

        var nextId = await _context.Mostovi.OrderBy(m => m.Id).Select(m => (int?)m.Id).FirstOrDefaultAsync();
        TempData["ImportSuccess"] = true;
        TempData["ImportMessage"] = "Most i sve pripadajuće prizme i merenja su obrisani.";
        if (nextId.HasValue)
            return RedirectToAction(nameof(Analiza), new { mostId = nextId.Value });
        return RedirectToAction(nameof(Analiza));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> AddPrism([FromForm] string ime, [FromForm] string uloga, [FromForm] int? mostId, [FromForm] double? yKoord, [FromForm] double? xKoord, [FromForm] double? zKoord)
    {
        if (string.IsNullOrWhiteSpace(ime))
            return BadRequest("Ime je obavezno.");
        var exists = await _context.LokacijePredef.AnyAsync(l => l.Ime == ime.Trim());
        if (exists)
            return BadRequest("Prizma sa ovim imenom već postoji.");

        if (mostId is null || !await _context.Mostovi.AnyAsync(m => m.Id == mostId.Value))
            return BadRequest("Nedostaje ili je nevažeći most. Osvežite stranicu i izaberite most.");

        var targetMostId = mostId.Value;
        var mostTip = await _context.Mostovi.AsNoTracking()
            .Where(m => m.Id == targetMostId)
            .Select(m => m.Tip)
            .FirstAsync();
        if (!PrizmaUloga.JeValidnaZaTip(mostTip, uloga))
            return BadRequest("Nevažeća uloga prizme za tip ovog mosta.");

        _context.LokacijePredef.Add(new LokacijaPredef
        {
            Ime = ime.Trim(),
            MostId = targetMostId,
            Uloga = uloga.Trim(),
            YKoord = yKoord,
            XKoord = xKoord,
            ZKoord = zKoord
        });
        await _context.SaveChangesAsync();
        return Ok();
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> DeletePrism([FromForm] string ime, [FromForm] int? expectedMostId)
    {
        if (string.IsNullOrWhiteSpace(ime))
            return BadRequest("Ime je obavezno.");
        var key = ime.Trim();
        var lokacija = await _context.LokacijePredef.FirstOrDefaultAsync(l => l.Ime == key);
        if (lokacija == null)
            return NotFound("Prizma nije pronađena.");
        if (expectedMostId.HasValue && lokacija.MostId != expectedMostId.Value)
            return BadRequest("Prizma ne pripada izabranom mostu.");

        // Obriši sva merenja pre lokacije da FK na lokacije_predef ne blokira brisanje.
        await _context.Merenja.Where(m => m.Ime == lokacija.Ime).ExecuteDeleteAsync();

        _context.LokacijePredef.Remove(lokacija);
        await _context.SaveChangesAsync();
        return Ok();
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> DeleteMeasurement([FromForm] string ime, [FromForm] DateTime dt, [FromForm] int? expectedMostId)
    {
        if (string.IsNullOrWhiteSpace(ime))
            return BadRequest("Prizma je obavezna.");

        var merenje = await _context.Merenja.FirstOrDefaultAsync(m => m.Ime == ime && m.Dt == dt);
        if (merenje == null)
            return NotFound("Merenje nije pronađeno.");

        var mostZaPrizmu = await _context.LokacijePredef.AsNoTracking()
            .Where(l => l.Ime == ime)
            .Select(l => (int?)l.MostId)
            .FirstOrDefaultAsync();
        if (expectedMostId.HasValue && mostZaPrizmu != expectedMostId.Value)
            return BadRequest("Merenje ne pripada izabranom mostu.");

        _context.Merenja.Remove(merenje);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Analiza), new { mostId = mostZaPrizmu, prizmaIme = ime });
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> EditPrism(
        [FromForm] string originalIme,
        [FromForm] string ime,
        [FromForm] string uloga,
        [FromForm] int? expectedMostId,
        [FromForm] double? yKoord,
        [FromForm] double? xKoord,
        [FromForm] double? zKoord)
    {
        if (string.IsNullOrWhiteSpace(originalIme))
        {
            TempData["ImportMessage"] = "Nije izabrana prizma za izmenu.";
            TempData["ImportSuccess"] = false;
            return RedirectToAction(nameof(Analiza));
        }

        var key = originalIme.Trim();
        var newIme = string.IsNullOrWhiteSpace(ime) ? key : ime.Trim();
        if (string.IsNullOrWhiteSpace(newIme))
        {
            TempData["ImportMessage"] = "Ime prizme je obavezno.";
            TempData["ImportSuccess"] = false;
            return RedirectToAction(nameof(Analiza), new { prizmaIme = key });
        }

        var lokacija = await _context.LokacijePredef.FirstOrDefaultAsync(l => l.Ime == key);
        if (lokacija == null)
        {
            TempData["ImportMessage"] = "Prizma nije pronađena.";
            TempData["ImportSuccess"] = false;
            return RedirectToAction(nameof(Analiza));
        }

        if (expectedMostId.HasValue && lokacija.MostId != expectedMostId.Value)
        {
            TempData["ImportMessage"] = "Prizma ne pripada izabranom mostu.";
            TempData["ImportSuccess"] = false;
            return RedirectToAction(nameof(Analiza), new { mostId = expectedMostId, prizmaIme = key });
        }

        var mostTip = await _context.Mostovi.AsNoTracking()
            .Where(m => m.Id == lokacija.MostId)
            .Select(m => m.Tip)
            .FirstAsync();
        if (!PrizmaUloga.JeValidnaZaTip(mostTip, uloga))
        {
            TempData["ImportMessage"] = "Nevažeća uloga prizme za tip ovog mosta.";
            TempData["ImportSuccess"] = false;
            return RedirectToAction(nameof(Analiza), new { mostId = lokacija.MostId, prizmaIme = key });
        }
        var ulogaNorm = uloga.Trim();

        var renamed = !string.Equals(key, newIme, StringComparison.Ordinal);
        if (renamed && await _context.LokacijePredef.AnyAsync(l => l.Ime == newIme))
        {
            TempData["ImportMessage"] = "Prizma sa tim imenom već postoji.";
            TempData["ImportSuccess"] = false;
            return RedirectToAction(nameof(Analiza), new { mostId = lokacija.MostId, prizmaIme = key });
        }

        var mostZaAlarm = await _context.Mostovi.AsNoTracking()
            .FirstAsync(m => m.Id == lokacija.MostId);
        var alarmLimit = EffectiveLimitAlarma(mostZaAlarm.LimitAlarma);

        var refX = xKoord ?? 0;
        var refY = yKoord ?? 0;
        var refZ = zKoord ?? 0;

        if (renamed)
        {
            var merenjaStara = await _context.Merenja.Where(m => m.Ime == key).ToListAsync();
            var merenjaNova = new List<Merenje>(merenjaStara.Count);
            foreach (var m in merenjaStara)
            {
                var novi = new Merenje
                {
                    Ime = newIme,
                    Dt = m.Dt,
                    Y = m.Y,
                    X = m.X,
                    Z = m.Z
                };
                ApplyRecalculatedDisplacement(novi, refX, refY, refZ, alarmLimit);
                merenjaNova.Add(novi);
            }

            _context.Merenja.RemoveRange(merenjaStara);
            _context.LokacijePredef.Remove(lokacija);
            _context.LokacijePredef.Add(new LokacijaPredef
            {
                Ime = newIme,
                MostId = lokacija.MostId,
                Uloga = ulogaNorm,
                YKoord = yKoord,
                XKoord = xKoord,
                ZKoord = zKoord
            });
            if (merenjaNova.Count > 0)
                _context.Merenja.AddRange(merenjaNova);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Analiza), new { mostId = lokacija.MostId, prizmaIme = newIme });
        }

        lokacija.YKoord = yKoord;
        lokacija.XKoord = xKoord;
        lokacija.ZKoord = zKoord;
        lokacija.Uloga = ulogaNorm;

        refX = lokacija.XKoord ?? 0;
        refY = lokacija.YKoord ?? 0;
        refZ = lokacija.ZKoord ?? 0;

        var merenja = await _context.Merenja.Where(m => m.Ime == key).ToListAsync();
        foreach (var m in merenja)
            ApplyRecalculatedDisplacement(m, refX, refY, refZ, alarmLimit);

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Analiza), new { mostId = lokacija.MostId, prizmaIme = key });
    }

    [HttpGet]
    public async Task<IActionResult> ExportToExcel(string? ime, int? mostId)
    {
        if (string.IsNullOrWhiteSpace(ime))
            return BadRequest("Prizma je obavezna.");

        if (mostId.HasValue)
        {
            var zaMost = await _context.LokacijePredef.AsNoTracking()
                .AnyAsync(l => l.Ime == ime && l.MostId == mostId.Value);
            if (!zaMost)
                return BadRequest("Prizma ne pripada izabranom mostu.");
        }

        var merenja = await _context.Merenja
            .AsNoTracking()
            .Where(m => m.Ime == ime)
            .OrderByDescending(m => m.Dt)
            .ToListAsync();

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Merenja");

        worksheet.Cell(1, 1).Value = "Datum";
        worksheet.Cell(1, 2).Value = "Ime";
        worksheet.Cell(1, 3).Value = "Y";
        worksheet.Cell(1, 4).Value = "X";
        worksheet.Cell(1, 5).Value = "Z";
        worksheet.Cell(1, 6).Value = "dY";
        worksheet.Cell(1, 7).Value = "dX";
        worksheet.Cell(1, 8).Value = "dZ";
        worksheet.Cell(1, 9).Value = "d3d";
        worksheet.Cell(1, 10).Value = "Status";

        var headerRange = worksheet.Range(1, 1, 1, 10);
        headerRange.Style.Font.Bold = true;

        var row = 2;
        foreach (var m in merenja)
        {
            worksheet.Cell(row, 1).Value = m.Dt.ToString("yyyy-MM-dd HH:mm");
            worksheet.Cell(row, 2).Value = m.Ime;
            worksheet.Cell(row, 3).Value = m.Y;
            worksheet.Cell(row, 4).Value = m.X;
            worksheet.Cell(row, 5).Value = m.Z;
            worksheet.Cell(row, 6).Value = m.Dy;
            worksheet.Cell(row, 7).Value = m.Dx;
            worksheet.Cell(row, 8).Value = m.Dz;
            worksheet.Cell(row, 9).Value = m.D3d;
            worksheet.Cell(row, 10).Value = m.Status;
            row++;
        }

        var dataRange = worksheet.Range(1, 1, row - 1, 10);
        dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        var datum = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var fileName = $"Izvestaj_{ime}_{datum}.xlsx";
        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    [HttpGet]
    public async Task<IActionResult> GetAlarmLimit(int? mostId)
    {
        if (mostId is { } mid && await _context.Mostovi.AsNoTracking().AnyAsync(m => m.Id == mid))
        {
            var lim = await _context.Mostovi.AsNoTracking()
                .Where(m => m.Id == mid)
                .Select(m => m.LimitAlarma)
                .FirstAsync();
            return Json(EffectiveLimitAlarma(lim));
        }

        return Json(DefaultLimitAlarma);
    }

    [HttpPost]
    public async Task<IActionResult> AddMerenje(
        [FromForm] string ime,
        [FromForm] int? mostId,
        [FromForm] double trenutniX,
        [FromForm] double trenutniY,
        [FromForm] double trenutniZ,
        [FromForm] DateTime dt)
    {
        if (string.IsNullOrWhiteSpace(ime))
            return BadRequest("Prizma je obavezna.");

        var lokacija = await _context.LokacijePredef.FirstOrDefaultAsync(l => l.Ime == ime);
        if (lokacija == null)
            return BadRequest("Prizma nije pronađena.");
        if (mostId.HasValue && lokacija.MostId != mostId.Value)
            return BadRequest("Prizma ne pripada izabranom mostu.");

        var xKoord = lokacija.XKoord ?? 0;
        var yKoord = lokacija.YKoord ?? 0;
        var zKoord = lokacija.ZKoord ?? 0;

        var dx = trenutniX - xKoord;
        var dy = trenutniY - yKoord;
        var dz = trenutniZ - zKoord;
        var d3d = Math.Sqrt(dx * dx + dy * dy + dz * dz);

        var mostZaMerenje = await _context.Mostovi.AsNoTracking()
            .FirstAsync(m => m.Id == lokacija.MostId);
        var alarmLimit = EffectiveLimitAlarma(mostZaMerenje.LimitAlarma);
        var limitSrelMm = EffectiveLimitSrel(mostZaMerenje.LimitSrel) * 1000.0;

        var status = ClassifyD3dStatus(d3d, alarmLimit);

        _context.Merenja.Add(new Merenje
        {
            Ime = ime,
            Dt = DateTime.SpecifyKind(dt, DateTimeKind.Utc),
            Y = trenutniY,
            X = trenutniX,
            Z = trenutniZ,
            Dy = dy,
            Dx = dx,
            Dz = dz,
            D3d = d3d,
            Status = status
        });
        await _context.SaveChangesAsync();

        if (await ParticipatesInVirtualSensorAsync(ime, HttpContext.RequestAborted))
            await TryNotifyVirtualSensorSrelCriticalAsync(lokacija.MostId, HttpContext.RequestAborted);

        if (d3d > alarmLimit)
        {
            await _emailService.SendAlarmEmailAsync(
                mostZaMerenje.Naziv,
                ime,
                d3d * 1000.0,
                alarmLimit * 1000.0,
                limitSrelMm,
                dt,
                HttpContext.RequestAborted);
        }

        return Ok();
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [RequestSizeLimit(52_428_800)]
    public async Task<IActionResult> ImportExcel(IFormFile? file, int mostId, string? returnIme)
    {
        var mostZaUvoz = await _context.Mostovi.AsNoTracking().FirstOrDefaultAsync(m => m.Id == mostId);
        if (mostZaUvoz == null)
        {
            TempData["ImportMessage"] = "Izabrani most ne postoji.";
            TempData["ImportSuccess"] = false;
            return RedirectToAction(nameof(Analiza), new { prizmaIme = returnIme });
        }

        if (file == null || file.Length == 0)
        {
            TempData["ImportMessage"] = "Nije izabran fajl.";
            TempData["ImportSuccess"] = false;
            return RedirectToAction(nameof(Analiza), new { mostId, prizmaIme = returnIme });
        }

        if (!Path.GetExtension(file.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            TempData["ImportMessage"] = "Dozvoljen je samo .xlsx fajl.";
            TempData["ImportSuccess"] = false;
            return RedirectToAction(nameof(Analiza), new { mostId, prizmaIme = returnIme });
        }

        var alarmLimit = EffectiveLimitAlarma(mostZaUvoz.LimitAlarma);

        var lokacijeZaMost = await _context.LokacijePredef.AsNoTracking()
            .Where(l => l.MostId == mostId)
            .ToDictionaryAsync(l => l.Ime, StringComparer.Ordinal);

        var toAdd = new List<Merenje>();
        var alarmCount = 0;
        var prizmeUAlarmu = new HashSet<string>(StringComparer.Ordinal);
        var preskoceneNepoznataPrizma = 0;
        var preskoceneImena = new HashSet<string>(StringComparer.Ordinal);

        try
        {
            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            stream.Position = 0;

            using var workbook = new XLWorkbook(stream);
            var ws = workbook.Worksheets.FirstOrDefault();
            if (ws == null)
            {
                TempData["ImportMessage"] = "Radni list nije pronađen.";
                TempData["ImportSuccess"] = false;
                return RedirectToAction(nameof(Analiza), new { mostId, prizmaIme = returnIme });
            }

            var headerRow = ws.FirstRowUsed();
            if (headerRow == null)
            {
                TempData["ImportMessage"] = "Tabela je prazna.";
                TempData["ImportSuccess"] = false;
                return RedirectToAction(nameof(Analiza), new { mostId, prizmaIme = returnIme });
            }

            var lastCol = ws.LastColumnUsed()?.ColumnNumber() ?? 0;
            var colMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (var c = 1; c <= lastCol; c++)
            {
                var h = ws.Cell(headerRow.RowNumber(), c).GetString().Trim();
                if (!string.IsNullOrEmpty(h))
                    colMap[h] = c;
            }

            static int? Col(Dictionary<string, int> m, params string[] names)
            {
                foreach (var n in names)
                {
                    if (m.TryGetValue(n, out var idx))
                        return idx;
                }
                return null;
            }

            var cIme = Col(colMap, "Ime", "IME", "ime");
            var cDatum = Col(colMap, "Datum", "DATUM", "datum", "Date", "Dt");
            var cX = Col(colMap, "X", "x");
            var cY = Col(colMap, "Y", "y");
            var cZ = Col(colMap, "Z", "z");

            if (cIme == null || cDatum == null || cX == null || cY == null || cZ == null)
            {
                TempData["ImportMessage"] = "Excel mora sadržati kolone: Ime, Datum, X, Y, Z.";
                TempData["ImportSuccess"] = false;
                return RedirectToAction(nameof(Analiza), new { mostId, prizmaIme = returnIme });
            }

            var lastRow = ws.LastRowUsed()?.RowNumber() ?? headerRow.RowNumber();
            for (var r = headerRow.RowNumber() + 1; r <= lastRow; r++)
            {
                var imeCell = ws.Cell(r, cIme.Value).GetString().Trim();
                if (string.IsNullOrWhiteSpace(imeCell))
                    continue;

                if (!lokacijeZaMost.TryGetValue(imeCell, out var lok))
                {
                    preskoceneNepoznataPrizma++;
                    preskoceneImena.Add(imeCell);
                    continue;
                }

                if (!TryParseExcelDateTime(ws.Cell(r, cDatum.Value), out var dtRow))
                    continue;

                if (!TryGetDoubleCell(ws.Cell(r, cX.Value), out var xVal) ||
                    !TryGetDoubleCell(ws.Cell(r, cY.Value), out var yVal) ||
                    !TryGetDoubleCell(ws.Cell(r, cZ.Value), out var zVal))
                    continue;

                var xF = lok.XKoord ?? 0;
                var yF = lok.YKoord ?? 0;
                var zF = lok.ZKoord ?? 0;

                var dx = xVal - xF;
                var dy = yVal - yF;
                var dz = zVal - zF;
                var d3d = Math.Sqrt(dx * dx + dy * dy + dz * dz);

                var status = ClassifyD3dStatus(d3d, alarmLimit);
                if (status == "ALARM")
                {
                    alarmCount++;
                    prizmeUAlarmu.Add(imeCell);
                }

                toAdd.Add(new Merenje
                {
                    Ime = imeCell,
                    Dt = DateTime.SpecifyKind(dtRow, DateTimeKind.Utc),
                    Y = yVal,
                    X = xVal,
                    Z = zVal,
                    Dy = dy,
                    Dx = dx,
                    Dz = dz,
                    D3d = d3d,
                    Status = status
                });
            }
        }
        catch (Exception ex)
        {
            TempData["ImportMessage"] = "Greška pri čitanju Excela: " + ex.Message;
            TempData["ImportSuccess"] = false;
            return RedirectToAction(nameof(Analiza), new { mostId, prizmaIme = returnIme });
        }

        if (toAdd.Count == 0)
        {
            var poruka = "Nije uvezen ni jedan validan red (proverite imena prizmi na izabranom mostu i format datuma).";
            if (preskoceneNepoznataPrizma > 0)
            {
                var uzorak = string.Join(", ", preskoceneImena.OrderBy(s => s, StringComparer.Ordinal).Take(12));
                var vise = preskoceneImena.Count > 12 ? "…" : "";
                poruka += $" Preskočeno {preskoceneNepoznataPrizma} red(ova): nema prizme za ovaj most (npr. {uzorak}{vise}).";
            }
            TempData["ImportMessage"] = poruka;
            TempData["ImportSuccess"] = false;
            return RedirectToAction(nameof(Analiza), new { mostId, prizmaIme = returnIme });
        }

        _context.Merenja.AddRange(toAdd);
        await _context.SaveChangesAsync();

        await TryNotifyVirtualSensorSrelCriticalAsync(mostId, HttpContext.RequestAborted);

        if (prizmeUAlarmu.Count > 0)
        {
            var listaAlarmPrizmi = prizmeUAlarmu.OrderBy(s => s, StringComparer.Ordinal).ToList();
            await _emailService.SendImportAlarmBatchSummaryAsync(
                mostZaUvoz.Naziv,
                listaAlarmPrizmi,
                HttpContext.RequestAborted);
        }

        await _emailService.SendBridgeImportSuccessEmailAsync(mostZaUvoz.Naziv, toAdd.Count, HttpContext.RequestAborted);

        TempData["ImportSuccess"] = true;
        var izvestaj = $"Uspešan uvoz za most „{mostZaUvoz.Naziv}”: uvezeno {toAdd.Count} merenja.";
        if (alarmCount > 0)
            izvestaj += $" Detektovano {alarmCount} alarma.";
        if (preskoceneNepoznataPrizma > 0)
        {
            var uzorak = string.Join(", ", preskoceneImena.OrderBy(s => s, StringComparer.Ordinal).Take(12));
            var vise = preskoceneImena.Count > 12 ? "…" : "";
            izvestaj += $" Preskočeno {preskoceneNepoznataPrizma} red(ova) — prizma nije na ovom mostu ({uzorak}{vise}).";
        }
        TempData["ImportMessage"] = izvestaj;

        return RedirectToAction(nameof(Analiza), new { mostId, prizmaIme = returnIme });
    }

    private static bool TryParseExcelDateTime(IXLCell cell, out DateTime dt)
    {
        dt = default;
        if (cell.IsEmpty())
            return false;

        try
        {
            if (cell.DataType == XLDataType.DateTime)
            {
                dt = cell.GetDateTime();
                return true;
            }

            if (cell.Value.IsNumber)
            {
                dt = DateTime.FromOADate(cell.GetDouble());
                return true;
            }

            var s = cell.GetString().Trim();
            if (string.IsNullOrEmpty(s))
                return false;

            if (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
                return true;
            if (DateTime.TryParse(s, CultureInfo.CurrentCulture, DateTimeStyles.None, out dt))
                return true;
        }
        catch
        {
            return false;
        }

        return false;
    }

    private static bool TryGetDoubleCell(IXLCell cell, out double value)
    {
        value = default;
        if (cell.IsEmpty())
            return false;

        if (cell.Value.IsNumber)
        {
            value = cell.GetDouble();
            return true;
        }

        var s = cell.GetString().Trim().Replace(',', '.');
        return double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out value);
    }

    [HttpPost]
    public async Task<IActionResult> UpdatePostavkeLimits([FromForm] int mostId, [FromForm] double alarmLimit, [FromForm] double limitSrel)
    {
        if (alarmLimit <= 0 || alarmLimit > 1000)
            return BadRequest("Granica alarma (d3d) mora biti između 0 i 1000.");
        if (limitSrel <= 0 || limitSrel > 1000)
            return BadRequest("Limit S_rel mora biti između 0 i 1000.");

        var most = await _context.Mostovi.FirstOrDefaultAsync(m => m.Id == mostId);
        if (most == null)
            return BadRequest("Most nije pronađen.");

        var previousLimitSrel = EffectiveLimitSrel(most.LimitSrel);
        most.LimitAlarma = alarmLimit;
        most.LimitSrel = limitSrel;

        var limitSrelChanged = Math.Abs(previousLimitSrel - limitSrel) > 1e-12;
        if (limitSrelChanged)
        {
            var ackKey = SrelAlarmAckUtcKeyForMost(mostId);
            var ackRow = await _context.Postavke.FirstOrDefaultAsync(x => x.Kljuc == ackKey);
            if (ackRow != null)
                _context.Postavke.Remove(ackRow);
        }

        await _context.SaveChangesAsync();
        return Ok();
    }
}

