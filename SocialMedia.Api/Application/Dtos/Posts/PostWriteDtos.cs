namespace SocialMedia.Api.Application.Dtos.Posts;

public class CreatePostWriteDto
{
    public string Text { get; set; } = string.Empty;
    public Guid? ReplyToPostId { get; set; }
    public List<string>? Tags { get; set; }
}

public class CreateReplyWriteDto
{
    public string Text { get; set; } = string.Empty;
    public List<string>? Tags { get; set; }
}

public class UpdatePostWriteDto
{
    public string Text { get; set; } = string.Empty;
    public List<string>? Tags { get; set; }
}
