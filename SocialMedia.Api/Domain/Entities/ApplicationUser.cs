using Microsoft.AspNetCore.Identity;

namespace SocialMedia.Api.Domain.Entities;

public class ApplicationUser : IdentityUser<Guid>
{
    public string? Bio { get; set; }
    public string? AvatarUrl { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public ICollection<Post> Posts { get; set; } = new HashSet<Post>();
    public ICollection<Comment> Comments { get; set; } = new HashSet<Comment>();
    public ICollection<PostLike> Likes { get; set; } = new HashSet<PostLike>();
    public ICollection<Follow> Following { get; set; } = new HashSet<Follow>();
    public ICollection<Follow> Followers { get; set; } = new HashSet<Follow>();
}
