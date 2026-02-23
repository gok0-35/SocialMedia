using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SocialMedia.Api.Application.Dtos.Common;
using SocialMedia.Api.Application.Dtos.Users;
using SocialMedia.Api.Domain.Entities;
using SocialMedia.Api.Infrastructure.Persistence;

namespace SocialMedia.Api.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public UsersController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("{userId:guid}")]
    public async Task<IActionResult> GetById([FromRoute] Guid userId)
    {
        UserProfileReadDto? user = await _dbContext.Users
            .AsNoTracking()
            .Where(x => x.Id == userId)
            .Select(x => new UserProfileReadDto
            {
                Id = x.Id,
                UserName = x.UserName ?? string.Empty,
                Bio = x.Bio,
                AvatarUrl = x.AvatarUrl,
                CreatedAtUtc = x.CreatedAtUtc,
                PostCount = x.Posts.Count,
                CommentCount = x.Comments.Count,
                LikeCount = x.Likes.Count,
                FollowersCount = x.Followers.Count,
                FollowingCount = x.Following.Count
            })
            .FirstOrDefaultAsync();

        if (user == null)
        {
            return NotFound("Kullanıcı bulunamadı.");
        }

        return Ok(user);
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        if (!TryGetCurrentUserId(User, out Guid currentUserId))
        {
            return Unauthorized("Geçersiz kullanıcı token'ı.");
        }

        MyProfileReadDto? me = await _dbContext.Users
            .AsNoTracking()
            .Where(x => x.Id == currentUserId)
            .Select(x => new MyProfileReadDto
            {
                Id = x.Id,
                UserName = x.UserName ?? string.Empty,
                Email = x.Email ?? string.Empty,
                Bio = x.Bio,
                AvatarUrl = x.AvatarUrl,
                CreatedAtUtc = x.CreatedAtUtc,
                PostCount = x.Posts.Count,
                CommentCount = x.Comments.Count,
                LikeCount = x.Likes.Count,
                FollowersCount = x.Followers.Count,
                FollowingCount = x.Following.Count
            })
            .FirstOrDefaultAsync();

        if (me == null)
        {
            return NotFound("Kullanıcı bulunamadı.");
        }

        return Ok(me);
    }

    [Authorize]
    [HttpPatch("me")]
    public async Task<IActionResult> UpdateMe([FromBody] UpdateMyProfileWriteDto request)
    {
        if (request == null) return BadRequest("Body boş olamaz.");

        if (!TryGetCurrentUserId(User, out Guid currentUserId))
        {
            return Unauthorized("Geçersiz kullanıcı token'ı.");
        }

        ApplicationUser? user = await _dbContext.Users.FirstOrDefaultAsync(x => x.Id == currentUserId);
        if (user == null)
        {
            return NotFound("Kullanıcı bulunamadı.");
        }

        if (request.Bio != null)
        {
            string bio = request.Bio.Trim();
            if (bio.Length > 500)
            {
                return BadRequest("Bio en fazla 500 karakter olabilir.");
            }

            user.Bio = bio.Length == 0 ? null : bio;
        }

        if (request.AvatarUrl != null)
        {
            string avatarUrl = request.AvatarUrl.Trim();
            if (avatarUrl.Length > 1000)
            {
                return BadRequest("AvatarUrl en fazla 1000 karakter olabilir.");
            }

            if (avatarUrl.Length == 0)
            {
                user.AvatarUrl = null;
            }
            else if (!Uri.IsWellFormedUriString(avatarUrl, UriKind.Absolute))
            {
                return BadRequest("AvatarUrl geçerli bir URL olmalı.");
            }
            else
            {
                user.AvatarUrl = avatarUrl;
            }
        }

        await _dbContext.SaveChangesAsync();

        return Ok(new MessageReadDto
        {
            Message = "Profil güncellendi."
        });
    }

    [HttpGet("{userId:guid}/posts")]
    public async Task<IActionResult> GetUserPosts([FromRoute] Guid userId, [FromQuery] int skip = 0, [FromQuery] int take = 20)
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

        List<UserPostReadDto> posts = await _dbContext.Posts
            .AsNoTracking()
            .Where(x => x.AuthorId == userId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip(skip)
            .Take(take)
            .Select(x => new UserPostReadDto
            {
                Id = x.Id,
                Text = x.Text,
                CreatedAtUtc = x.CreatedAtUtc,
                ReplyToPostId = x.ReplyToPostId,
                LikeCount = x.Likes.Count,
                CommentCount = x.Comments.Count
            })
            .ToListAsync();

        return Ok(posts);
    }

    [HttpGet("{userId:guid}/comments")]
    public async Task<IActionResult> GetUserComments([FromRoute] Guid userId, [FromQuery] int skip = 0, [FromQuery] int take = 20)
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

        List<UserCommentReadDto> comments = await _dbContext.Comments
            .AsNoTracking()
            .Where(x => x.AuthorId == userId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip(skip)
            .Take(take)
            .Select(x => new UserCommentReadDto
            {
                Id = x.Id,
                PostId = x.PostId,
                Body = x.Body,
                ParentCommentId = x.ParentCommentId,
                CreatedAtUtc = x.CreatedAtUtc
            })
            .ToListAsync();

        return Ok(comments);
    }

    [HttpGet("{userId:guid}/liked-posts")]
    public async Task<IActionResult> GetLikedPosts([FromRoute] Guid userId, [FromQuery] int skip = 0, [FromQuery] int take = 20)
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

        List<UserLikedPostReadDto> likedPosts = await _dbContext.PostLikes
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip(skip)
            .Take(take)
            .Select(x => new UserLikedPostReadDto
            {
                PostId = x.PostId,
                AuthorId = x.Post.AuthorId,
                AuthorUserName = x.Post.Author.UserName ?? string.Empty,
                Text = x.Post.Text,
                PostCreatedAtUtc = x.Post.CreatedAtUtc,
                LikedAtUtc = x.CreatedAtUtc
            })
            .ToListAsync();

        return Ok(likedPosts);
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
