using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.IdentityModel.Tokens;
using SocialMedia.Api.Domain.Entities;
using SocialMedia.Api.Infrastructure.Auth;
using SocialMedia.Api.Infrastructure.Email;

namespace SocialMedia.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly JwtSettings _jwtSettings;
    private readonly IEmailSender _emailSender;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        JwtSettings jwtSettings,
        IEmailSender emailSender)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtSettings = jwtSettings;
        _emailSender = emailSender;
    }

    public class RegisterRequest
    {
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class LoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class ResendConfirmationEmailRequest
    {
        public string Email { get; set; } = string.Empty;
    }

    public class ForgotPasswordRequest
    {
        public string Email { get; set; } = string.Empty;
    }

    public class ResetPasswordRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }

    public class AuthResponse
    {
        public string Token { get; set; } = string.Empty;
        public DateTimeOffset ExpiresAtUtc { get; set; }
        public Guid UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (request == null) return BadRequest("Body boş olamaz.");
        if (string.IsNullOrWhiteSpace(request.Email)) return BadRequest("Email zorunlu.");
        if (string.IsNullOrWhiteSpace(request.Password)) return BadRequest("Password zorunlu.");

        string email = request.Email.Trim();
        string userName = string.IsNullOrWhiteSpace(request.UserName) ? email : request.UserName.Trim();

        ApplicationUser user = new ApplicationUser();
        user.Id = Guid.NewGuid();
        user.UserName = userName;
        user.Email = email;

        IdentityResult createResult = await _userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            return BadRequest(createResult.Errors);
        }

        await SendEmailConfirmationAsync(user);

        return Ok(new
        {
            message = "Kayıt başarılı. Giriş yapmadan önce email adresini onaylamalısın."
        });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (request == null) return BadRequest("Body boş olamaz.");
        if (string.IsNullOrWhiteSpace(request.Email)) return BadRequest("Email zorunlu.");
        if (string.IsNullOrWhiteSpace(request.Password)) return BadRequest("Password zorunlu.");

        string email = request.Email.Trim();

        ApplicationUser? user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            return Unauthorized("Email veya şifre hatalı.");
        }

        if (!await _userManager.IsEmailConfirmedAsync(user))
        {
            return Unauthorized("Email onaylanmadan giriş yapılamaz.");
        }

        Microsoft.AspNetCore.Identity.SignInResult signInResult = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: false);
        if (!signInResult.Succeeded)
        {
            return Unauthorized("Email veya şifre hatalı.");
        }

        AuthResponse response = CreateToken(user);
        return Ok(response);
    }

    [HttpGet("confirm-email")]
    public async Task<IActionResult> ConfirmEmail([FromQuery] Guid userId, [FromQuery] string token)
    {
        if (userId == Guid.Empty) return BadRequest("Geçersiz kullanıcı.");
        if (string.IsNullOrWhiteSpace(token)) return BadRequest("Token zorunlu.");

        ApplicationUser? user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            return BadRequest("Geçersiz kullanıcı.");
        }

        if (!TryDecodeToken(token, out string decodedToken))
        {
            return BadRequest("Geçersiz token.");
        }

        IdentityResult result = await _userManager.ConfirmEmailAsync(user, decodedToken);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        return Ok(new { message = "Email başarıyla onaylandı." });
    }

    [HttpPost("resend-confirmation-email")]
    public async Task<IActionResult> ResendConfirmationEmail([FromBody] ResendConfirmationEmailRequest request)
    {
        if (request == null) return BadRequest("Body boş olamaz.");
        if (string.IsNullOrWhiteSpace(request.Email)) return BadRequest("Email zorunlu.");

        string email = request.Email.Trim();
        ApplicationUser? user = await _userManager.FindByEmailAsync(email);

        if (user != null && !await _userManager.IsEmailConfirmedAsync(user))
        {
            await SendEmailConfirmationAsync(user);
        }

        return Ok(new { message = "Hesap varsa onay emaili gönderildi." });
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        if (request == null) return BadRequest("Body boş olamaz.");
        if (string.IsNullOrWhiteSpace(request.Email)) return BadRequest("Email zorunlu.");

        string email = request.Email.Trim();
        ApplicationUser? user = await _userManager.FindByEmailAsync(email);

        if (user != null && await _userManager.IsEmailConfirmedAsync(user))
        {
            string token = await _userManager.GeneratePasswordResetTokenAsync(user);
            string encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

            string htmlBody =
                "Şifre sıfırlama isteği aldık.<br/>" +
                "Aşağıdaki token ile <code>/api/auth/reset-password</code> endpoint'ine POST atabilirsin.<br/><br/>" +
                $"Email: <b>{email}</b><br/>" +
                $"Token: <code>{encodedToken}</code>";

            await _emailSender.SendEmailAsync(email, "SocialMedia - Şifre Sıfırlama", htmlBody);
        }

        return Ok(new { message = "Hesap varsa şifre sıfırlama emaili gönderildi." });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        if (request == null) return BadRequest("Body boş olamaz.");
        if (string.IsNullOrWhiteSpace(request.Email)) return BadRequest("Email zorunlu.");
        if (string.IsNullOrWhiteSpace(request.Token)) return BadRequest("Token zorunlu.");
        if (string.IsNullOrWhiteSpace(request.NewPassword)) return BadRequest("NewPassword zorunlu.");

        string email = request.Email.Trim();
        ApplicationUser? user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            return BadRequest("Geçersiz token veya email.");
        }

        if (!TryDecodeToken(request.Token, out string decodedToken))
        {
            return BadRequest("Geçersiz token.");
        }

        IdentityResult result = await _userManager.ResetPasswordAsync(user, decodedToken, request.NewPassword);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        return Ok(new { message = "Şifre başarıyla güncellendi." });
    }

    private AuthResponse CreateToken(ApplicationUser user)
    {
        DateTimeOffset expiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(_jwtSettings.ExpiresMinutes);

        List<Claim> claims = new List<Claim>();
        claims.Add(new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()));
        claims.Add(new Claim(ClaimTypes.Name, user.UserName ?? string.Empty));

        byte[] keyBytes = Encoding.UTF8.GetBytes(_jwtSettings.Key);
        SymmetricSecurityKey securityKey = new SymmetricSecurityKey(keyBytes);

        SigningCredentials credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        JwtSecurityToken token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAtUtc.UtcDateTime,
            signingCredentials: credentials);

        string tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        AuthResponse response = new AuthResponse();
        response.Token = tokenString;
        response.ExpiresAtUtc = expiresAtUtc;
        response.UserId = user.Id;
        response.UserName = user.UserName ?? string.Empty;
        response.Email = user.Email ?? string.Empty;

        return response;
    }

    private async Task SendEmailConfirmationAsync(ApplicationUser user)
    {
        if (string.IsNullOrWhiteSpace(user.Email))
        {
            return;
        }

        string token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        string encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

        string? confirmationUrl = Url.ActionLink(
            action: nameof(ConfirmEmail),
            controller: "Auth",
            values: new { userId = user.Id, token = encodedToken },
            protocol: Request.Scheme);

        string htmlBody =
            "Hesabını onaylamak için aşağıdaki linke tıkla:<br/><br/>" +
            $"<a href=\"{confirmationUrl}\">{confirmationUrl}</a>";

        await _emailSender.SendEmailAsync(user.Email, "SocialMedia - Email Onayı", htmlBody);
    }

    private static bool TryDecodeToken(string encodedToken, out string decodedToken)
    {
        decodedToken = string.Empty;

        try
        {
            byte[] bytes = WebEncoders.Base64UrlDecode(encodedToken);
            decodedToken = Encoding.UTF8.GetString(bytes);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
