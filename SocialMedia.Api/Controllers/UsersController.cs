using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialMedia.Api.Application.Dtos.Users;
using SocialMedia.Api.Application.Services;
using SocialMedia.Api.Application.Services.Abstractions;

namespace SocialMedia.Api.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet("{userId:guid}")]
    public async Task<IActionResult> GetById([FromRoute] Guid userId)
    {
        var result = await _userService.GetByIdAsync(userId);
        if (!result.IsSuccess)
        {
            return this.ToActionResult(result.Error!);
        }

        return Ok(result.Data);
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        if (!TryGetCurrentUserId(User, out Guid currentUserId))
        {
            return Unauthorized("Geçersiz kullanıcı token'ı.");
        }

        var result = await _userService.GetMeAsync(currentUserId);
        if (!result.IsSuccess)
        {
            return this.ToActionResult(result.Error!);
        }

        return Ok(result.Data);
    }

    [Authorize]
    [HttpPatch("me")]
    public async Task<IActionResult> UpdateMe([FromBody] UpdateMyProfileWriteDto request)
    {
        if (!TryGetCurrentUserId(User, out Guid currentUserId))
        {
            return Unauthorized("Geçersiz kullanıcı token'ı.");
        }

        var result = await _userService.UpdateMeAsync(currentUserId, request);
        if (!result.IsSuccess)
        {
            return this.ToActionResult(result.Error!);
        }

        return Ok(result.Data);
    }

    [HttpGet("{userId:guid}/posts")]
    public async Task<IActionResult> GetUserPosts([FromRoute] Guid userId, [FromQuery] int skip = 0, [FromQuery] int take = 20)
    {
        var result = await _userService.GetUserPostsAsync(userId, skip, take);
        if (!result.IsSuccess)
        {
            return this.ToActionResult(result.Error!);
        }

        return Ok(result.Data);
    }

    [HttpGet("{userId:guid}/comments")]
    public async Task<IActionResult> GetUserComments([FromRoute] Guid userId, [FromQuery] int skip = 0, [FromQuery] int take = 20)
    {
        var result = await _userService.GetUserCommentsAsync(userId, skip, take);
        if (!result.IsSuccess)
        {
            return this.ToActionResult(result.Error!);
        }

        return Ok(result.Data);
    }

    [HttpGet("{userId:guid}/liked-posts")]
    public async Task<IActionResult> GetLikedPosts([FromRoute] Guid userId, [FromQuery] int skip = 0, [FromQuery] int take = 20)
    {
        var result = await _userService.GetLikedPostsAsync(userId, skip, take);
        if (!result.IsSuccess)
        {
            return this.ToActionResult(result.Error!);
        }

        return Ok(result.Data);
    }

    private static bool TryGetCurrentUserId(ClaimsPrincipal user, out Guid userId)
    {
        userId = Guid.Empty;

        string? userIdValue = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdValue, out userId);
    }
}
