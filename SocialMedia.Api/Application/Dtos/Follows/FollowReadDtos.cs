namespace SocialMedia.Api.Application.Dtos.Follows;

public class FollowUserReadDto
{
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public DateTimeOffset FollowedAtUtc { get; set; }
}

public class FollowListReadDto
{
    public Guid UserId { get; set; }
    public int TotalCount { get; set; }
    public List<FollowUserReadDto> Items { get; set; } = new();
}
