using System.Net;
using System.Net.Mail;

namespace SocialMedia.Api.Infrastructure.Email;

public sealed class SmtpEmailSender : IEmailSender
{
    private readonly EmailSettings _emailSettings;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(EmailSettings emailSettings, ILogger<SmtpEmailSender> logger)
    {
        _emailSettings = emailSettings;
        _logger = logger;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
    {
        if (string.IsNullOrWhiteSpace(toEmail))
        {
            throw new ArgumentException("Recipient email cannot be empty.", nameof(toEmail));
        }

        using MailMessage message = new MailMessage
        {
            From = new MailAddress(_emailSettings.FromAddress, _emailSettings.FromName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };

        message.To.Add(toEmail.Trim());

        using SmtpClient smtpClient = new SmtpClient(_emailSettings.SmtpHost, _emailSettings.SmtpPort)
        {
            EnableSsl = _emailSettings.UseSsl,
            Credentials = new NetworkCredential(_emailSettings.SmtpUser, _emailSettings.SmtpPassword)
        };

        try
        {
            await smtpClient.SendMailAsync(message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {ToEmail}", toEmail);
            throw;
        }
    }
}
