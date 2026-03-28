namespace ThesisWebApp.Services;

public interface IEmailService
{
    Task SendAlarmEmailAsync(string mostNaziv, string ime, double d3dMm, double limitD3dMm, double limitSrelMm, DateTime measuredAt, CancellationToken cancellationToken = default);

    Task SendBulkImportSummaryEmailAsync(int importedCount, int alarmCount, CancellationToken cancellationToken = default);

    /// <summary>Obaveštenje o uspešnom uvozu Excela za konkretan most.</summary>
    Task SendBridgeImportSuccessEmailAsync(string mostNaziv, int importedCount, CancellationToken cancellationToken = default);

    /// <summary>Virtuelni senzor — S_rel u mm, limit u mm.</summary>
    Task SendSrelCriticalEmailAsync(string mostNaziv, double srelMm, double limitSrelMm, double limitAlarmaMm, CancellationToken cancellationToken = default);
}
