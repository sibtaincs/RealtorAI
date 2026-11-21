using System.Net;
using System.Net.Mail;

namespace RealEstateAI.Api.Services;

public interface IEmailSender
{
    Task SendAsync(string to, string subject, string body);
}

/// <summary>
/// Sends alert email when SMTP is configured; otherwise logs the intent so local
/// development stays offline like the deterministic AI agent.
/// </summary>
public class SmtpEmailSender : IEmailSender
{
    private readonly IConfiguration _config;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IConfiguration config, ILogger<SmtpEmailSender> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendAsync(string to, string subject, string body)
    {
        var host = _config["Smtp:Host"];
        if (string.IsNullOrWhiteSpace(host))
        {
            _logger.LogInformation("Email alert skipped — SMTP not configured. To={To}, Subject={Subject}", to, subject);
            return;
        }

        var from = _config["Smtp:From"] ?? host;
        var port = int.TryParse(_config["Smtp:Port"], out var parsed) ? parsed : 587;
        var useSsl = bool.TryParse(_config["Smtp:UseSsl"], out var ssl) && ssl;
        var user = _config["Smtp:User"];

        using var message = new MailMessage(from, to, subject, body);
        using var client = new SmtpClient(host, port) { EnableSsl = useSsl };
        if (!string.IsNullOrWhiteSpace(user))
            client.Credentials = new NetworkCredential(user, _config["Smtp:Password"]);

        await client.SendMailAsync(message);
    }
}
