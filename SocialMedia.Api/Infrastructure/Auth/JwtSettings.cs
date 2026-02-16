namespace SocialMedia.Api.Infrastructure.Auth;

public sealed class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Issuer { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
    public string Key { get; init; } = string.Empty;
    public int ExpiresMinutes { get; init; }

    public static JwtSettings FromConfiguration(IConfiguration configuration)
    {
        IConfigurationSection section = configuration.GetSection(SectionName);

        string? issuer = section["Issuer"];
        string? audience = section["Audience"];
        string? key = section["Key"];
        string? expiresMinutesRaw = section["ExpiresMinutes"];

        if (string.IsNullOrWhiteSpace(issuer))
        {
            throw new InvalidOperationException("Missing configuration: Jwt:Issuer");
        }

        if (string.IsNullOrWhiteSpace(audience))
        {
            throw new InvalidOperationException("Missing configuration: Jwt:Audience");
        }

        if (string.IsNullOrWhiteSpace(key))
        {
            throw new InvalidOperationException("Missing configuration: Jwt:Key");
        }

        if (key.Length < 32)
        {
            throw new InvalidOperationException("Invalid configuration: Jwt:Key must be at least 32 characters.");
        }

        if (!int.TryParse(expiresMinutesRaw, out int expiresMinutes) || expiresMinutes <= 0)
        {
            throw new InvalidOperationException("Invalid configuration: Jwt:ExpiresMinutes must be a positive integer.");
        }

        return new JwtSettings
        {
            Issuer = issuer.Trim(),
            Audience = audience.Trim(),
            Key = key,
            ExpiresMinutes = expiresMinutes
        };
    }
}
