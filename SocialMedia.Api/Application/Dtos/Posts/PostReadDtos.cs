namespace SocialMedia.Api.Application.Dtos.Posts;

public class PostSummaryReadDto
{
    public Guid Id { get; set; }
    public Guid AuthorId { get; set; }
    public string AuthorUserName { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public Guid? ReplyToPostId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public int LikeCount { get; set; }
    public int CommentCount { get; set; }
    public int ReplyCount { get; set; }
    public List<string> Tags { get; set; } = new();
}

public class LikeUserReadDto
{
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public DateTimeOffset LikedAtUtc { get; set; }
}

public class PostLikesReadDto
{
    public Guid PostId { get; set; }
    public int TotalCount { get; set; }
    public List<LikeUserReadDto> Items { get; set; } = new();
}

public class CreatedPostReadDto
{
    public string Message { get; set; } = string.Empty;
    public Guid PostId { get; set; }
}

