namespace SocialMedia.Api.Infrastructure.Email;

public interface IEmailSender
{
    Task SendEmailAsync(string toEmail, string subject, string htmlBody);
}
