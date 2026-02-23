using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialMedia.Api.Application.Dtos.Comments;
using SocialMedia.Api.Application.Services;
using SocialMedia.Api.Application.Services.Abstractions;

namespace SocialMedia.Api.Controllers;

[ApiController]
[Route("api/posts/{postId:guid}/comments")]
public class CommentsController : ControllerBase
{
    private readonly ICommentService _commentService;

    public CommentsController(ICommentService commentService)
    {
        _commentService = commentService;
    }

    [HttpGet]
    public async Task<IActionResult> GetByPost([FromRoute] Guid postId, [FromQuery] int skip = 0, [FromQuery] int take = 20)
    {
        var result = await _commentService.GetByPostAsync(postId, skip, take);
        if (!result.IsSuccess)
        {
            return this.ToActionResult(result.Error!);
        }

        return Ok(result.Data);
    }

    [HttpGet("/api/comments/{commentId:guid}")]
    public async Task<IActionResult> GetById([FromRoute] Guid commentId)
    {
        var result = await _commentService.GetByIdAsync(commentId);
        if (!result.IsSuccess)
        {
            return this.ToActionResult(result.Error!);
        }

        return Ok(result.Data);
    }

    [HttpGet("/api/comments/{commentId:guid}/children")]
    public async Task<IActionResult> GetChildren([FromRoute] Guid commentId, [FromQuery] int skip = 0, [FromQuery] int take = 20)
    {
        var result = await _commentService.GetChildrenAsync(commentId, skip, take);
        if (!result.IsSuccess)
        {
            return this.ToActionResult(result.Error!);
        }

        return Ok(result.Data);
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create([FromRoute] Guid postId, [FromBody] CreateCommentWriteDto request)
    {
        if (!TryGetCurrentUserId(User, out Guid currentUserId))
        {
            return Unauthorized("Geçersiz kullanıcı token'ı.");
        }

        var result = await _commentService.CreateAsync(currentUserId, postId, request);
        if (!result.IsSuccess)
        {
            return this.ToActionResult(result.Error!);
        }

        CreatedCommentReadDto response = result.Data!;
        return Created($"/api/comments/{response.CommentId}", response);
    }

    [Authorize]
    [HttpDelete("/api/comments/{commentId:guid}")]
    public async Task<IActionResult> Delete([FromRoute] Guid commentId)
    {
        if (!TryGetCurrentUserId(User, out Guid currentUserId))
        {
            return Unauthorized("Geçersiz kullanıcı token'ı.");
        }

        var result = await _commentService.DeleteAsync(currentUserId, commentId);
        if (!result.IsSuccess)
        {
            return this.ToActionResult(result.Error!);
        }

        return Ok(result.Data);
    }

    [Authorize]
    [HttpPatch("/api/comments/{commentId:guid}")]
    public async Task<IActionResult> Update([FromRoute] Guid commentId, [FromBody] UpdateCommentWriteDto request)
    {
        if (!TryGetCurrentUserId(User, out Guid currentUserId))
        {
            return Unauthorized("Geçersiz kullanıcı token'ı.");
        }

        var result = await _commentService.UpdateAsync(currentUserId, commentId, request);
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
