using System.Net;
using System.Net.Mail;
using GodForge.Application.Common.Interfaces;
using GodForge.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GodForge.Infrastructure.Services;

public sealed class EmailService : IEmailService
{
    private readonly EmailSettings _settings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<EmailSettings> settings, ILogger<EmailService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SendEmailAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Attempting to send email to {To} with subject: {Subject}", to, subject);

        using var client = new SmtpClient(_settings.Smtp.Host, _settings.Smtp.Port)
        {
            Credentials = new NetworkCredential(_settings.Smtp.UserName, _settings.Smtp.Password),
            EnableSsl = _settings.Smtp.EnableSsl
        };

        var mailMessage = new MailMessage
        {
            From = new MailAddress(_settings.Smtp.FromEmail, _settings.Smtp.FromName),
            Subject = subject,
            Body = body,
            IsBodyHtml = true
        };

        mailMessage.To.Add(to);

        // System.Net.Mail doesn't directly support passing CancellationToken to SendMailAsync in old versions,
        // but .NET Core's SendMailAsync accepts CancellationToken in newer versions.
        // Let's use the overload with CancellationToken or just await SendMailAsync(mailMessage) if not available,
        // or check .NET 9 features. In .NET 9 / .NET Standard, SendMailAsync has a CancellationToken overload.
        await client.SendMailAsync(mailMessage, cancellationToken);

        _logger.LogInformation("Email sent successfully to {To}", to);
    }
}
