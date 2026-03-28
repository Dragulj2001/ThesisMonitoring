using System.Globalization;
using System.Net;
using System.Text;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using ThesisWebApp.Models;

namespace ThesisWebApp.Services;

public class EmailService : IEmailService
{
    private readonly EmailSettings _settings;

    public EmailService(IOptions<EmailSettings> options)
    {
        _settings = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Povezuje se na SMTP, po potrebi se autentifikuje (korisničko ime: Username ili SenderEmail, lozinka: Password iz EmailSettings u appsettings.json) i šalje poruku.
    /// Greške se loguju u konzolu i ne bacaju dalje.
    /// </summary>
    private async Task TrySendAsync(MimeMessage message, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.ReceiverEmail) ||
            string.IsNullOrWhiteSpace(_settings.SmtpServer) ||
            string.IsNullOrWhiteSpace(_settings.SenderEmail))
        {
            Console.WriteLine("[EmailService] Slanje e-pošte preskočeno: proverite EmailSettings (ReceiverEmail, SmtpServer, SenderEmail) u appsettings.json.");
            return;
        }

        try
        {
            using var client = new SmtpClient();
            await client.ConnectAsync(_settings.SmtpServer, _settings.SmtpPort, SecureSocketOptions.StartTls, cancellationToken);

            if (!string.IsNullOrWhiteSpace(_settings.Password))
            {
                var authUser = !string.IsNullOrWhiteSpace(_settings.Username)
                    ? _settings.Username
                    : _settings.SenderEmail;
                await client.AuthenticateAsync(authUser, _settings.Password, cancellationToken);
            }
            else if (!string.IsNullOrWhiteSpace(_settings.Username))
            {
                await client.AuthenticateAsync(_settings.Username, string.Empty, cancellationToken);
            }

            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EmailService] Greška pri slanju e-pošte (Subject: {message.Subject}): {ex.Message}");
            Console.WriteLine(ex.ToString());
        }
    }

    public async Task SendAlarmEmailAsync(string mostNaziv, string ime, double d3dMm, double limitD3dMm, double limitSrelMm, DateTime measuredAt, CancellationToken cancellationToken = default)
    {
        var d3dStr = d3dMm.ToString("F5", CultureInfo.InvariantCulture);
        var limD3dStr = limitD3dMm.ToString("F5", CultureInfo.InvariantCulture);
        var limSrelStr = limitSrelMm.ToString("F5", CultureInfo.InvariantCulture);
        var vreme = measuredAt.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
        var imeEncoded = WebUtility.HtmlEncode(ime);
        var mostEnc = WebUtility.HtmlEncode(mostNaziv);

        var body = new StringBuilder();
        body.Append("<p>");
        body.Append($"Most <strong>{mostEnc}</strong> — prizma <strong>{imeEncoded}</strong> je prešla granicu d3d! ");
        body.Append($"Izmereni pomeraj d3d iznosi <strong>{d3dStr} mm</strong> ");
        body.Append($"(limit za ovaj most: <strong>{limD3dStr} mm</strong>; limit |S_rel|: <strong>{limSrelStr} mm</strong>). ");
        body.Append($"Vreme merenja: <strong>{WebUtility.HtmlEncode(vreme)}</strong>.");
        body.Append("</p>");

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_settings.SenderName, _settings.SenderEmail));
        message.To.Add(MailboxAddress.Parse(_settings.ReceiverEmail));
        message.Subject = "HITNO: Detektovan Alarm na objektu!";
        message.Body = new TextPart("html") { Text = body.ToString() };

        await TrySendAsync(message, cancellationToken);
    }

    public async Task SendSrelCriticalEmailAsync(string mostNaziv, double srelMm, double limitSrelMm, double limitAlarmaMm, CancellationToken cancellationToken = default)
    {
        var vStr = srelMm.ToString("F3", CultureInfo.InvariantCulture);
        var lSrelStr = limitSrelMm.ToString("F3", CultureInfo.InvariantCulture);
        var lArmStr = limitAlarmaMm.ToString("F3", CultureInfo.InvariantCulture);
        var mostEnc = WebUtility.HtmlEncode(mostNaziv);

        var body =
            "<p>Most <strong>" + mostEnc + "</strong>. Virtuelni senzor: relativno ulegnuće <strong>" +
            WebUtility.HtmlEncode(vStr) + " mm</strong> prelazi limit |S_rel| za ovaj most (<strong>" +
            WebUtility.HtmlEncode(lSrelStr) + " mm</strong>). Granica alarma d3d na istom mostu: <strong>" +
            WebUtility.HtmlEncode(lArmStr) + " mm</strong>. Proverite stabilnost konstrukcije!</p>";

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_settings.SenderName, _settings.SenderEmail));
        message.To.Add(MailboxAddress.Parse(_settings.ReceiverEmail));
        message.Subject = "KRITIČNO: Deformacija konstrukcije mosta!";
        message.Body = new TextPart("html") { Text = body };

        await TrySendAsync(message, cancellationToken);
    }

    public async Task SendBulkImportSummaryEmailAsync(int importedCount, int alarmCount, CancellationToken cancellationToken = default)
    {
        var body = $"<p>Uvezeno je <strong>{importedCount}</strong> merenja, od čega je detektovano <strong>{alarmCount}</strong> alarma.</p>";

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_settings.SenderName, _settings.SenderEmail));
        message.To.Add(MailboxAddress.Parse(_settings.ReceiverEmail));
        message.Subject = "Import merenja – zbirni izveštaj";
        message.Body = new TextPart("html") { Text = body };

        await TrySendAsync(message, cancellationToken);
    }

    public async Task SendBridgeImportSuccessEmailAsync(string mostNaziv, int importedCount, CancellationToken cancellationToken = default)
    {
        var nazivEnc = WebUtility.HtmlEncode(mostNaziv);
        var body =
            "<p>Uspešan uvoz za <strong>" + nazivEnc + "</strong>. Uvezeno <strong>" +
            importedCount.ToString(CultureInfo.InvariantCulture) + "</strong> merenja.</p>";

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_settings.SenderName, _settings.SenderEmail));
        message.To.Add(MailboxAddress.Parse(_settings.ReceiverEmail));
        message.Subject = "Uvoz merenja — " + mostNaziv;
        message.Body = new TextPart("html") { Text = body };

        await TrySendAsync(message, cancellationToken);
    }
}
