using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialMedia.Api.Application.Services;
using SocialMedia.Api.Application.Services.Abstractions;

namespace SocialMedia.Api.Controllers;

[ApiController]
[Route("api/follows")]
public class FollowsController : ControllerBase
{
    private readonly IFollowService _followService;

    public FollowsController(IFollowService followService)
    {
        _followService = followService;
    }

    [Authorize]
    [HttpPost("{followingUserId:guid}")]
    public async Task<IActionResult> Follow([FromRoute] Guid followingUserId)
    {
        if (!TryGetCurrentUserId(User, out Guid currentUserId))
        {
            return Unauthorized("Geçersiz kullanıcı token'ı.");
        }

        var result = await _followService.FollowAsync(currentUserId, followingUserId);
        if (!result.IsSuccess)
        {
            return this.ToActionResult(result.Error!);
        }

        return Ok(result.Data);
    }

    [Authorize]
    [HttpDelete("{followingUserId:guid}")]
    public async Task<IActionResult> Unfollow([FromRoute] Guid followingUserId)
    {
        if (!TryGetCurrentUserId(User, out Guid currentUserId))
        {
            return Unauthorized("Geçersiz kullanıcı token'ı.");
        }

        var result = await _followService.UnfollowAsync(currentUserId, followingUserId);
        if (!result.IsSuccess)
        {
            return this.ToActionResult(result.Error!);
        }

        return Ok(result.Data);
    }

    [HttpGet("{userId:guid}/followers")]
    public async Task<IActionResult> GetFollowers([FromRoute] Guid userId, [FromQuery] int skip = 0, [FromQuery] int take = 20)
    {
        var result = await _followService.GetFollowersAsync(userId, skip, take);
        if (!result.IsSuccess)
        {
            return this.ToActionResult(result.Error!);
        }

        return Ok(result.Data);
    }

    [HttpGet("{userId:guid}/following")]
    public async Task<IActionResult> GetFollowing([FromRoute] Guid userId, [FromQuery] int skip = 0, [FromQuery] int take = 20)
    {
        var result = await _followService.GetFollowingAsync(userId, skip, take);
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
