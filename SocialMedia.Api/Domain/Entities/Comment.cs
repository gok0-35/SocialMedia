using System.ComponentModel.DataAnnotations;

namespace SocialMedia.Api.Domain.Entities;

public class Comment : AuditableEntity
{
    public Guid Id { get; set; }
    public Guid PostId { get; set; }
    public Post Post { get; set; } = default!;
    public Guid AuthorId { get; set; }
    public ApplicationUser Author { get; set; } = default!;
    [MaxLength(2000)]
    public string Body { get; set; } = string.Empty;
    public Guid? ParentCommentId { get; set; }
    public Comment? ParentComment { get; set; }
    public ICollection<Comment> Children { get; set; } = new HashSet<Comment>();
}
