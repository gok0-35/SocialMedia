namespace SocialMedia.Api.Application.Dtos.Auth;

public class RegisterWriteDto
{
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginWriteDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class ResendConfirmationEmailWriteDto
{
    public string Email { get; set; } = string.Empty;
}

public class ForgotPasswordWriteDto
{
    public string Email { get; set; } = string.Empty;
}

public class ResetPasswordWriteDto
{
    public string Email { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}
