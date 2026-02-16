namespace SocialMedia.Api.Infrastructure.Email;

public sealed class EmailSettings
{
    public const string SectionName = "Email";

    public string SmtpHost { get; init; } = string.Empty;
    public int SmtpPort { get; init; }
    public string SmtpUser { get; init; } = string.Empty;
    public string SmtpPassword { get; init; } = string.Empty;
    public bool UseSsl { get; init; }
    public string FromAddress { get; init; } = string.Empty;
    public string FromName { get; init; } = string.Empty;

    public static EmailSettings FromConfiguration(IConfiguration configuration)
    {
        IConfigurationSection section = configuration.GetSection(SectionName);

        string? smtpHost = section["SmtpHost"];
        string? smtpPortRaw = section["SmtpPort"];
        string? smtpUser = section["SmtpUser"];
        string? smtpPassword = section["SmtpPassword"];
        string? useSslRaw = section["UseSsl"];
        string? fromAddress = section["FromAddress"];
        string? fromName = section["FromName"];

        if (string.IsNullOrWhiteSpace(smtpHost))
        {
            throw new InvalidOperationException("Missing configuration: Email:SmtpHost");
        }

        if (!int.TryParse(smtpPortRaw, out int smtpPort) || smtpPort <= 0)
        {
            throw new InvalidOperationException("Invalid configuration: Email:SmtpPort must be a positive integer.");
        }

        if (string.IsNullOrWhiteSpace(smtpUser))
        {
            throw new InvalidOperationException("Missing configuration: Email:SmtpUser");
        }

        if (string.IsNullOrWhiteSpace(smtpPassword))
        {
            throw new InvalidOperationException("Missing configuration: Email:SmtpPassword");
        }

        if (!bool.TryParse(useSslRaw, out bool useSsl))
        {
            throw new InvalidOperationException("Invalid configuration: Email:UseSsl must be true or false.");
        }

        if (string.IsNullOrWhiteSpace(fromAddress))
        {
            throw new InvalidOperationException("Missing configuration: Email:FromAddress");
        }

        if (string.IsNullOrWhiteSpace(fromName))
        {
            throw new InvalidOperationException("Missing configuration: Email:FromName");
        }

        return new EmailSettings
        {
            SmtpHost = smtpHost.Trim(),
            SmtpPort = smtpPort,
            SmtpUser = smtpUser.Trim(),
            SmtpPassword = smtpPassword,
            UseSsl = useSsl,
            FromAddress = fromAddress.Trim(),
            FromName = fromName.Trim()
        };
    }
}
