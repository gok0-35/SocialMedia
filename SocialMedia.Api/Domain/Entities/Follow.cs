namespace SocialMedia.Api.Domain.Entities;

public class Follow : AuditableEntity
{
    public Guid FollowerId { get; set; }
    public ApplicationUser Follower { get; set; } = default!;
    public Guid FollowingId { get; set; }
    public ApplicationUser Following { get; set; } = default!;
}
