namespace SocialMedia.Api.Domain.Entities;

public class PostLike : AuditableEntity
{
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = default!;
    public Guid PostId { get; set; }
    public Post Post { get; set; } = default!;
}
