using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialMedia.Api.Application.Dtos.Me;

namespace SocialMedia.Api.Controllers;

[ApiController]
[Route("api/me")]
public class MeController : ControllerBase
{
    [Authorize]
    [HttpGet]
    public IActionResult GetMe()
    {
        string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        string? userName = User.FindFirstValue(ClaimTypes.Name);

        return Ok(new MeReadDto
        {
            UserId = userId,
            UserName = userName
        });
    }
}
