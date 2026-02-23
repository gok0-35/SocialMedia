namespace SocialMedia.Api.Application.Dtos.Comments;

public class CreateCommentWriteDto
{
    public string Body { get; set; } = string.Empty;
    public Guid? ParentCommentId { get; set; }
}

public class UpdateCommentWriteDto
{
    public string Body { get; set; } = string.Empty;
}
