using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialMedia.Api.Application.Dtos.Posts;
using SocialMedia.Api.Application.Services;
using SocialMedia.Api.Application.Services.Abstractions;

namespace SocialMedia.Api.Controllers;

[ApiController]
[Route("api/posts")]
public class PostsController : ControllerBase
{
    private readonly IPostService _postService;

    public PostsController(IPostService postService)
    {
        _postService = postService;
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePostWriteDto request)
    {
        if (!TryGetCurrentUserId(User, out Guid currentUserId))
        {
            return Unauthorized("Geçersiz kullanıcı token'ı.");
        }

        var result = await _postService.CreateAsync(currentUserId, request);
        if (!result.IsSuccess)
        {
            return this.ToActionResult(result.Error!);
        }

        CreatedPostReadDto response = result.Data!;
        return CreatedAtAction(nameof(GetById), new { postId = response.PostId }, response);
    }

    [Authorize]
    [HttpPost("{postId:guid}/replies")]
    public async Task<IActionResult> CreateReply([FromRoute] Guid postId, [FromBody] CreateReplyWriteDto request)
    {
        if (!TryGetCurrentUserId(User, out Guid currentUserId))
        {
            return Unauthorized("Geçersiz kullanıcı token'ı.");
        }

        var result = await _postService.CreateReplyAsync(currentUserId, postId, request);
        if (!result.IsSuccess)
        {
            return this.ToActionResult(result.Error!);
        }

        CreatedPostReadDto response = result.Data!;
        return CreatedAtAction(nameof(GetById), new { postId = response.PostId }, response);
    }

    [Authorize]
    [HttpPatch("{postId:guid}")]
    public async Task<IActionResult> Update([FromRoute] Guid postId, [FromBody] UpdatePostWriteDto request)
    {
        if (!TryGetCurrentUserId(User, out Guid currentUserId))
        {
            return Unauthorized("Geçersiz kullanıcı token'ı.");
        }

        var result = await _postService.UpdateAsync(currentUserId, postId, request);
        if (!result.IsSuccess)
        {
            return this.ToActionResult(result.Error!);
        }

        return Ok(result.Data);
    }

    [HttpGet]
    public async Task<IActionResult> GetPosts(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20,
        [FromQuery] Guid? authorId = null,
        [FromQuery] string? tag = null)
    {
        var result = await _postService.GetPostsAsync(skip, take, authorId, tag);
        if (!result.IsSuccess)
        {
            return this.ToActionResult(result.Error!);
        }

        return Ok(result.Data);
    }

    [Authorize]
    [HttpGet("feed")]
    public async Task<IActionResult> GetFeed([FromQuery] int skip = 0, [FromQuery] int take = 20)
    {
        if (!TryGetCurrentUserId(User, out Guid currentUserId))
        {
            return Unauthorized("Geçersiz kullanıcı token'ı.");
        }

        var result = await _postService.GetFeedAsync(currentUserId, skip, take);
        if (!result.IsSuccess)
        {
            return this.ToActionResult(result.Error!);
        }

        return Ok(result.Data);
    }

    [HttpGet("{postId:guid}")]
    public async Task<IActionResult> GetById([FromRoute] Guid postId)
    {
        var result = await _postService.GetByIdAsync(postId);
        if (!result.IsSuccess)
        {
            return this.ToActionResult(result.Error!);
        }

        return Ok(result.Data);
    }

    [HttpGet("{postId:guid}/replies")]
    public async Task<IActionResult> GetReplies([FromRoute] Guid postId, [FromQuery] int skip = 0, [FromQuery] int take = 20)
    {
        var result = await _postService.GetRepliesAsync(postId, skip, take);
        if (!result.IsSuccess)
        {
            return this.ToActionResult(result.Error!);
        }

        return Ok(result.Data);
    }

    [Authorize]
    [HttpDelete("{postId:guid}")]
    public async Task<IActionResult> Delete([FromRoute] Guid postId)
    {
        if (!TryGetCurrentUserId(User, out Guid currentUserId))
        {
            return Unauthorized("Geçersiz kullanıcı token'ı.");
        }

        var result = await _postService.DeleteAsync(currentUserId, postId);
        if (!result.IsSuccess)
        {
            return this.ToActionResult(result.Error!);
        }

        return Ok(result.Data);
    }

    [Authorize]
    [HttpPost("{postId:guid}/like")]
    public async Task<IActionResult> Like([FromRoute] Guid postId)
    {
        if (!TryGetCurrentUserId(User, out Guid currentUserId))
        {
            return Unauthorized("Geçersiz kullanıcı token'ı.");
        }

        var result = await _postService.LikeAsync(currentUserId, postId);
        if (!result.IsSuccess)
        {
            return this.ToActionResult(result.Error!);
        }

        return Ok(result.Data);
    }

    [Authorize]
    [HttpDelete("{postId:guid}/like")]
    public async Task<IActionResult> Unlike([FromRoute] Guid postId)
    {
        if (!TryGetCurrentUserId(User, out Guid currentUserId))
        {
            return Unauthorized("Geçersiz kullanıcı token'ı.");
        }

        var result = await _postService.UnlikeAsync(currentUserId, postId);
        if (!result.IsSuccess)
        {
            return this.ToActionResult(result.Error!);
        }

        return Ok(result.Data);
    }

    [HttpGet("{postId:guid}/likes")]
    public async Task<IActionResult> GetLikes([FromRoute] Guid postId, [FromQuery] int skip = 0, [FromQuery] int take = 20)
    {
        var result = await _postService.GetLikesAsync(postId, skip, take);
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
