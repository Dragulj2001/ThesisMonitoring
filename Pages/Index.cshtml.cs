using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ThesisWebApp.Data;

namespace ThesisWebApp.Pages;

public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public IndexModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public int PrismCount { get; set; }
    public int MeasurementCount { get; set; }
    public string StatusText { get; set; } = "Sve OK"; // ALARM | Upozorenje | Sve OK
    public string StatusCssClass { get; set; } = "text-success"; // text-danger | text-warning | text-success

    public async Task OnGetAsync()
    {
        PrismCount = await _context.LokacijePredef.CountAsync();
        MeasurementCount = await _context.Merenja.CountAsync();

        var imeToLimitAlarma = await _context.LokacijePredef
            .Join(_context.Mostovi, l => l.MostId, mo => mo.Id, (l, mo) => new { l.Ime, mo.LimitAlarma })
            .ToDictionaryAsync(x => x.Ime, x => x.LimitAlarma <= 0 ? 0.15 : x.LimitAlarma);

        var latestMeasurements = await _context.Merenja
            .GroupBy(m => m.Ime)
            .Select(g => g.OrderByDescending(m => m.Dt).First())
            .ToListAsync();

        static bool OverLimit(double d3d, double lim) => d3d > lim;
        static bool InWarnBand(double d3d, double lim) => d3d >= lim * 0.7 && d3d <= lim;

        var hasAlarm = latestMeasurements.Any(m =>
            m.D3d is { } d &&
            imeToLimitAlarma.TryGetValue(m.Ime, out var lim) &&
            OverLimit(d, lim));

        var hasWarn = latestMeasurements.Any(m =>
            m.D3d is { } d &&
            imeToLimitAlarma.TryGetValue(m.Ime, out var lim) &&
            InWarnBand(d, lim));

        if (hasAlarm)
        {
            StatusText = "ALARM";
            StatusCssClass = "text-danger";
        }
        else if (hasWarn)
        {
            StatusText = "Upozorenje";
            StatusCssClass = "text-warning";
        }
        else
        {
            StatusText = "Sve OK";
            StatusCssClass = "text-success";
        }
    }
}
