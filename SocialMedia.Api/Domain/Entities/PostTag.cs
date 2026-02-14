namespace SocialMedia.Api.Domain.Entities;

public class PostTag : AuditableEntity
{
    public Guid PostId { get; set; }
    public Post Post { get; set; } = default!;
    public Guid TagId { get; set; }
    public Tag Tag { get; set; } = default!;
}
