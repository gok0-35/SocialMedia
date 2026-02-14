using System.ComponentModel.DataAnnotations;

namespace SocialMedia.Api.Domain.Entities;

public class Tag : AuditableEntity
{
    public Guid Id { get; set; }
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;
    public ICollection<PostTag> PostTags { get; set; } = new HashSet<PostTag>();
}
