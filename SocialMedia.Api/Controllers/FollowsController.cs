using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SocialMedia.Api.Domain.Entities;
using SocialMedia.Api.Infrastructure.Persistence;

namespace SocialMedia.Api.Controllers;

[ApiController]
[Route("api/follows")]
public class FollowsController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public FollowsController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public class FollowUserResponse
    {
        public Guid UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public DateTimeOffset FollowedAtUtc { get; set; }
    }

    [Authorize]
    [HttpPost("{followingUserId:guid}")]
    public async Task<IActionResult> Follow([FromRoute] Guid followingUserId)
    {
        if (!TryGetCurrentUserId(User, out Guid currentUserId))
        {
            return Unauthorized("Geçersiz kullanıcı token'ı.");
        }

        if (followingUserId == currentUserId)
        {
            return BadRequest("Kendini takip edemezsin.");
        }

        bool followingUserExists = await _dbContext.Users.AnyAsync(x => x.Id == followingUserId);
        if (!followingUserExists)
        {
            return NotFound("Takip edilecek kullanıcı bulunamadı.");
        }

        Follow? existingFollow = await _dbContext.Follows.FindAsync(currentUserId, followingUserId);
        if (existingFollow != null)
        {
            return Ok(new { message = "Kullanıcı zaten takip ediliyor." });
        }

        _dbContext.Follows.Add(new Follow
        {
            FollowerId = currentUserId,
            FollowingId = followingUserId
        });

        await _dbContext.SaveChangesAsync();

        return Ok(new { message = "Kullanıcı takip edildi." });
    }

    [Authorize]
    [HttpDelete("{followingUserId:guid}")]
    public async Task<IActionResult> Unfollow([FromRoute] Guid followingUserId)
    {
        if (!TryGetCurrentUserId(User, out Guid currentUserId))
        {
            return Unauthorized("Geçersiz kullanıcı token'ı.");
        }

        Follow? existingFollow = await _dbContext.Follows.FindAsync(currentUserId, followingUserId);
        if (existingFollow == null)
        {
            return Ok(new { message = "Kullanıcı zaten takip edilmiyor." });
        }

        _dbContext.Follows.Remove(existingFollow);
        await _dbContext.SaveChangesAsync();

        return Ok(new { message = "Takip bırakıldı." });
    }

    [HttpGet("{userId:guid}/followers")]
    public async Task<IActionResult> GetFollowers([FromRoute] Guid userId, [FromQuery] int skip = 0, [FromQuery] int take = 20)
    {
        if (!TryValidatePagination(skip, take, out IActionResult? errorResult))
        {
            return errorResult!;
        }

        bool userExists = await _dbContext.Users.AnyAsync(x => x.Id == userId);
        if (!userExists)
        {
            return NotFound("Kullanıcı bulunamadı.");
        }

        int totalCount = await _dbContext.Follows.CountAsync(x => x.FollowingId == userId);

        List<FollowUserResponse> followers = await _dbContext.Follows
            .AsNoTracking()
            .Where(x => x.FollowingId == userId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip(skip)
            .Take(take)
            .Select(x => new FollowUserResponse
            {
                UserId = x.FollowerId,
                UserName = x.Follower.UserName ?? string.Empty,
                FollowedAtUtc = x.CreatedAtUtc
            })
            .ToListAsync();

        return Ok(new
        {
            userId,
            totalCount,
            items = followers
        });
    }

    [HttpGet("{userId:guid}/following")]
    public async Task<IActionResult> GetFollowing([FromRoute] Guid userId, [FromQuery] int skip = 0, [FromQuery] int take = 20)
    {
        if (!TryValidatePagination(skip, take, out IActionResult? errorResult))
        {
            return errorResult!;
        }

        bool userExists = await _dbContext.Users.AnyAsync(x => x.Id == userId);
        if (!userExists)
        {
            return NotFound("Kullanıcı bulunamadı.");
        }

        int totalCount = await _dbContext.Follows.CountAsync(x => x.FollowerId == userId);

        List<FollowUserResponse> following = await _dbContext.Follows
            .AsNoTracking()
            .Where(x => x.FollowerId == userId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip(skip)
            .Take(take)
            .Select(x => new FollowUserResponse
            {
                UserId = x.FollowingId,
                UserName = x.Following.UserName ?? string.Empty,
                FollowedAtUtc = x.CreatedAtUtc
            })
            .ToListAsync();

        return Ok(new
        {
            userId,
            totalCount,
            items = following
        });
    }

    private static bool TryValidatePagination(int skip, int take, out IActionResult? errorResult)
    {
        errorResult = null;

        if (skip < 0)
        {
            errorResult = new BadRequestObjectResult("skip 0 veya daha büyük olmalı.");
            return false;
        }

        if (take <= 0 || take > 100)
        {
            errorResult = new BadRequestObjectResult("take 1 ile 100 arasında olmalı.");
            return false;
        }

        return true;
    }

    private static bool TryGetCurrentUserId(ClaimsPrincipal user, out Guid userId)
    {
        userId = Guid.Empty;

        string? userIdValue = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdValue, out userId);
    }
}
