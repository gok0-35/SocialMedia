namespace SocialMedia.Api.Application.Dtos.Tags;

public class TagSummaryReadDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public int PostCount { get; set; }
}

public class TagPostReadDto
{
    public Guid Id { get; set; }
    public Guid AuthorId { get; set; }
    public string AuthorUserName { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid? ReplyToPostId { get; set; }
    public int LikeCount { get; set; }
    public int CommentCount { get; set; }
}

public class TrendingTagReadDto
{
    public Guid TagId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int PostCount { get; set; }
}

public class TagPostsReadDto
{
    public string Tag { get; set; } = string.Empty;
    public List<TagPostReadDto> Items { get; set; } = new();
}
