using System.ComponentModel.DataAnnotations;

namespace SocialMedia.Api.Domain.Entities;

public class Post : AuditableEntity
{
    public Guid Id { get; set; }
    public Guid AuthorId { get; set; }
    public ApplicationUser Author { get; set; } = default!;
    [MaxLength(280)]
    public string Text { get; set; } = string.Empty;
    public Guid? ReplyToPostId { get; set; }
    public Post? ReplyToPost { get; set; }
    public ICollection<Post> Replies { get; set; } = new HashSet<Post>();
    public ICollection<Comment> Comments { get; set; } = new HashSet<Comment>();
    public ICollection<PostLike> Likes { get; set; } = new HashSet<PostLike>();
    public ICollection<PostTag> PostTags { get; set; } = new HashSet<PostTag>();
}
