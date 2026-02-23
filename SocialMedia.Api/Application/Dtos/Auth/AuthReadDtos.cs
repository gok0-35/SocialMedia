namespace SocialMedia.Api.Application.Dtos.Auth;

public class AuthReadDto
{
    public string Token { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAtUtc { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

