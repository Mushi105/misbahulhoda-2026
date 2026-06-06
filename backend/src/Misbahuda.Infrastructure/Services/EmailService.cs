using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace Misbahuda.Infrastructure.Services;

public interface IEmailService
{
    Task<bool> SendAsync(string toEmail, string toName, string subject, string htmlBody);
    Task<int> BroadcastAsync(IEnumerable<(string Email, string Name)> recipients, string subject, string htmlBody);
}

public class EmailService(IConfiguration config, ILogger<EmailService> logger) : IEmailService
{
    private readonly string  _host       = config["Email:SmtpHost"]    ?? "smtp.gmail.com";
    private readonly int     _port       = int.TryParse(config["Email:SmtpPort"], out var p) ? p : 587;
    private readonly bool    _useSsl     = bool.TryParse(config["Email:UseSsl"], out var s) && s;
    private readonly string? _user       = config["Email:SenderEmail"];
    private readonly string? _pass       = config["Email:SmtpPassword"];
    private readonly string  _fromName   = config["Email:SenderName"]   ?? "Misbah ul Hoda";
    private readonly string? _fromAddr   = config["Email:SenderEmail"];

    private bool IsConfigured =>
        !string.IsNullOrEmpty(_user) && !string.IsNullOrEmpty(_pass);

    public async Task<bool> SendAsync(string toEmail, string toName, string subject, string htmlBody)
    {
        if (!IsConfigured)
        {
            logger.LogWarning("Email not configured. Skipping email to {Email}", toEmail);
            return false;
        }

        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_fromName, _fromAddr));
            message.To.Add(new MailboxAddress(toName, toEmail));
            message.Subject = subject;
            message.Body = new TextPart("html") { Text = htmlBody };

            using var client = new SmtpClient();

            // Port 587 → STARTTLS, Port 465 → SSL, else Auto
            var socketOptions = _port == 465
                ? SecureSocketOptions.SslOnConnect
                : _useSsl
                    ? SecureSocketOptions.StartTls
                    : SecureSocketOptions.Auto;

            await client.ConnectAsync(_host, _port, socketOptions);
            await client.AuthenticateAsync(_user, _pass);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            logger.LogInformation("Email sent to {Email}", toEmail);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Email failed for {Email}", toEmail);
            return false;
        }
    }

    public async Task<int> BroadcastAsync(IEnumerable<(string Email, string Name)> recipients, string subject, string htmlBody)
    {
        if (!IsConfigured)
        {
            logger.LogWarning("Email not configured. Broadcast skipped.");
            return 0;
        }

        var tasks = recipients.Select(r => SendAsync(r.Email, r.Name, subject, htmlBody));
        var results = await Task.WhenAll(tasks);
        return results.Count(r => r);
    }
}
