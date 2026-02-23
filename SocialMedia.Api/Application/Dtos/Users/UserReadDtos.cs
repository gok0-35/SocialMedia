namespace SocialMedia.Api.Application.Dtos.Users;

public class UserProfileReadDto
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public string? AvatarUrl { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public int PostCount { get; set; }
    public int CommentCount { get; set; }
    public int LikeCount { get; set; }
    public int FollowersCount { get; set; }
    public int FollowingCount { get; set; }
}

public class MyProfileReadDto : UserProfileReadDto
{
    public string Email { get; set; } = string.Empty;
}

public class UserPostReadDto
{
    public Guid Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid? ReplyToPostId { get; set; }
    public int LikeCount { get; set; }
    public int CommentCount { get; set; }
}

public class UserCommentReadDto
{
    public Guid Id { get; set; }
    public Guid PostId { get; set; }
    public string Body { get; set; } = string.Empty;
    public Guid? ParentCommentId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}

public class UserLikedPostReadDto
{
    public Guid PostId { get; set; }
    public Guid AuthorId { get; set; }
    public string AuthorUserName { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public DateTimeOffset PostCreatedAtUtc { get; set; }
    public DateTimeOffset LikedAtUtc { get; set; }
}
