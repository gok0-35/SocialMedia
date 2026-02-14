namespace SocialMedia.Api.Domain.Entities;

public abstract class AuditableEntity
{
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
