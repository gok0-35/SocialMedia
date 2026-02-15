using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using SocialMedia.Api.Domain.Entities;

namespace SocialMedia.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IConfiguration _configuration;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _configuration = configuration;
    }

    public class RegisterRequest
    {
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class LoginRequest
    {
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class AuthResponse
    {
        public string Token { get; set; } = string.Empty;
        public DateTimeOffset ExpiresAtUtc { get; set; }
        public Guid UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (request == null) return BadRequest("Body boş olamaz.");
        if (string.IsNullOrWhiteSpace(request.UserName)) return BadRequest("UserName zorunlu.");
        if (string.IsNullOrWhiteSpace(request.Password)) return BadRequest("Password zorunlu.");

        string userName = request.UserName.Trim();

        ApplicationUser user = new ApplicationUser();
        user.Id = Guid.NewGuid();
        user.UserName = userName;

        IdentityResult createResult = await _userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            return BadRequest(createResult.Errors);
        }

        AuthResponse response = CreateToken(user);
        return Ok(response);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (request == null) return BadRequest("Body boş olamaz.");
        if (string.IsNullOrWhiteSpace(request.UserName)) return BadRequest("UserName zorunlu.");
        if (string.IsNullOrWhiteSpace(request.Password)) return BadRequest("Password zorunlu.");

        string userName = request.UserName.Trim();

        ApplicationUser? user = await _userManager.FindByNameAsync(userName);
        if (user == null)
        {
            return Unauthorized("Kullanıcı adı veya şifre hatalı.");
        }

        Microsoft.AspNetCore.Identity.SignInResult signInResult = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: false);
        if (!signInResult.Succeeded)
        {
            return Unauthorized("Kullanıcı adı veya şifre hatalı.");
        }

        AuthResponse response = CreateToken(user);
        return Ok(response);
    }

    private AuthResponse CreateToken(ApplicationUser user)
    {
        string issuer = _configuration["Jwt:Issuer"]!;
        string audience = _configuration["Jwt:Audience"]!;
        string key = _configuration["Jwt:Key"]!;
        int expiresMinutes = int.Parse(_configuration["Jwt:ExpiresMinutes"]!);

        DateTimeOffset expiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(expiresMinutes);

        List<Claim> claims = new List<Claim>();
        claims.Add(new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()));
        claims.Add(new Claim(ClaimTypes.Name, user.UserName ?? string.Empty));

        byte[] keyBytes = Encoding.UTF8.GetBytes(key);
        SymmetricSecurityKey securityKey = new SymmetricSecurityKey(keyBytes);

        SigningCredentials credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        JwtSecurityToken token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
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

        return response;
    }
}
