using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SocialMedia.Api.Application.Dtos.Comments;
using SocialMedia.Api.Application.Dtos.Common;
using SocialMedia.Api.Domain.Entities;
using SocialMedia.Api.Infrastructure.Persistence;

namespace SocialMedia.Api.Controllers;

[ApiController]
[Route("api/posts/{postId:guid}/comments")]
public class CommentsController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public CommentsController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> GetByPost([FromRoute] Guid postId, [FromQuery] int skip = 0, [FromQuery] int take = 20)
    {
        if (!TryValidatePagination(skip, take, out IActionResult? errorResult))
        {
            return errorResult!;
        }

        bool postExists = await _dbContext.Posts.AnyAsync(x => x.Id == postId);
        if (!postExists)
        {
            return NotFound("Post bulunamadı.");
        }

        List<CommentReadDto> comments = await _dbContext.Comments
            .AsNoTracking()
            .Where(x => x.PostId == postId)
            .OrderBy(x => x.CreatedAtUtc)
            .Skip(skip)
            .Take(take)
            .Select(x => new CommentReadDto
            {
                Id = x.Id,
                PostId = x.PostId,
                AuthorId = x.AuthorId,
                AuthorUserName = x.Author.UserName ?? string.Empty,
                Body = x.Body,
                ParentCommentId = x.ParentCommentId,
                CreatedAtUtc = x.CreatedAtUtc,
                ChildrenCount = x.Children.Count
            })
            .ToListAsync();

        return Ok(comments);
    }

    [HttpGet("/api/comments/{commentId:guid}")]
    public async Task<IActionResult> GetById([FromRoute] Guid commentId)
    {
        CommentReadDto? comment = await _dbContext.Comments
            .AsNoTracking()
            .Where(x => x.Id == commentId)
            .Select(x => new CommentReadDto
            {
                Id = x.Id,
                PostId = x.PostId,
                AuthorId = x.AuthorId,
                AuthorUserName = x.Author.UserName ?? string.Empty,
                Body = x.Body,
                ParentCommentId = x.ParentCommentId,
                CreatedAtUtc = x.CreatedAtUtc,
                ChildrenCount = x.Children.Count
            })
            .FirstOrDefaultAsync();

        if (comment == null)
        {
            return NotFound("Yorum bulunamadı.");
        }

        return Ok(comment);
    }

    [HttpGet("/api/comments/{commentId:guid}/children")]
    public async Task<IActionResult> GetChildren([FromRoute] Guid commentId, [FromQuery] int skip = 0, [FromQuery] int take = 20)
    {
        if (!TryValidatePagination(skip, take, out IActionResult? errorResult))
        {
            return errorResult!;
        }

        bool commentExists = await _dbContext.Comments.AnyAsync(x => x.Id == commentId);
        if (!commentExists)
        {
            return NotFound("Yorum bulunamadı.");
        }

        List<CommentReadDto> children = await _dbContext.Comments
            .AsNoTracking()
            .Where(x => x.ParentCommentId == commentId)
            .OrderBy(x => x.CreatedAtUtc)
            .Skip(skip)
            .Take(take)
            .Select(x => new CommentReadDto
            {
                Id = x.Id,
                PostId = x.PostId,
                AuthorId = x.AuthorId,
                AuthorUserName = x.Author.UserName ?? string.Empty,
                Body = x.Body,
                ParentCommentId = x.ParentCommentId,
                CreatedAtUtc = x.CreatedAtUtc,
                ChildrenCount = x.Children.Count
            })
            .ToListAsync();

        return Ok(children);
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create([FromRoute] Guid postId, [FromBody] CreateCommentWriteDto request)
    {
        if (request == null) return BadRequest("Body boş olamaz.");
        if (string.IsNullOrWhiteSpace(request.Body)) return BadRequest("Body zorunlu.");

        string body = request.Body.Trim();
        if (body.Length > 2000) return BadRequest("Yorum en fazla 2000 karakter olabilir.");

        if (!TryGetCurrentUserId(User, out Guid currentUserId))
        {
            return Unauthorized("Geçersiz kullanıcı token'ı.");
        }

        bool postExists = await _dbContext.Posts.AnyAsync(x => x.Id == postId);
        if (!postExists)
        {
            return NotFound("Post bulunamadı.");
        }

        if (request.ParentCommentId.HasValue)
        {
            Comment? parentComment = await _dbContext.Comments
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == request.ParentCommentId.Value);

            if (parentComment == null)
            {
                return BadRequest("ParentCommentId geçersiz.");
            }

            if (parentComment.PostId != postId)
            {
                return BadRequest("ParentComment aynı post içinde olmalı.");
            }
        }

        Comment comment = new Comment
        {
            Id = Guid.NewGuid(),
            PostId = postId,
            AuthorId = currentUserId,
            Body = body,
            ParentCommentId = request.ParentCommentId
        };

        _dbContext.Comments.Add(comment);
        await _dbContext.SaveChangesAsync();

        return Created($"/api/comments/{comment.Id}", new CreatedCommentReadDto
        {
            Message = "Yorum eklendi.",
            CommentId = comment.Id
        });
    }

    [Authorize]
    [HttpDelete("/api/comments/{commentId:guid}")]
    public async Task<IActionResult> Delete([FromRoute] Guid commentId)
    {
        if (!TryGetCurrentUserId(User, out Guid currentUserId))
        {
            return Unauthorized("Geçersiz kullanıcı token'ı.");
        }

        Comment? comment = await _dbContext.Comments.FirstOrDefaultAsync(x => x.Id == commentId);
        if (comment == null)
        {
            return NotFound("Yorum bulunamadı.");
        }

        if (comment.AuthorId != currentUserId)
        {
            return Forbid();
        }

        _dbContext.Comments.Remove(comment);
        await _dbContext.SaveChangesAsync();

        return Ok(new MessageReadDto
        {
            Message = "Yorum silindi."
        });
    }

    [Authorize]
    [HttpPatch("/api/comments/{commentId:guid}")]
    public async Task<IActionResult> Update([FromRoute] Guid commentId, [FromBody] UpdateCommentWriteDto request)
    {
        if (request == null) return BadRequest("Body boş olamaz.");
        if (string.IsNullOrWhiteSpace(request.Body)) return BadRequest("Body zorunlu.");

        string body = request.Body.Trim();
        if (body.Length > 2000) return BadRequest("Yorum en fazla 2000 karakter olabilir.");

        if (!TryGetCurrentUserId(User, out Guid currentUserId))
        {
            return Unauthorized("Geçersiz kullanıcı token'ı.");
        }

        Comment? comment = await _dbContext.Comments.FirstOrDefaultAsync(x => x.Id == commentId);
        if (comment == null)
        {
            return NotFound("Yorum bulunamadı.");
        }

        if (comment.AuthorId != currentUserId)
        {
            return Forbid();
        }

        comment.Body = body;
        await _dbContext.SaveChangesAsync();

        return Ok(new MessageReadDto
        {
            Message = "Yorum güncellendi."
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
