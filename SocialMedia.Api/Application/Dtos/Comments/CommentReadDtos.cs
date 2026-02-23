namespace SocialMedia.Api.Application.Dtos.Comments;

public class CommentReadDto
{
    public Guid Id { get; set; }
    public Guid PostId { get; set; }
    public Guid AuthorId { get; set; }
    public string AuthorUserName { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public Guid? ParentCommentId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public int ChildrenCount { get; set; }
}

public class CreatedCommentReadDto
{
    public string Message { get; set; } = string.Empty;
    public Guid CommentId { get; set; }
}

