using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SocialMedia.Api.Application.Dtos.Common;
using SocialMedia.Api.Application.Dtos.Posts;
using SocialMedia.Api.Domain.Entities;
using SocialMedia.Api.Infrastructure.Persistence;

namespace SocialMedia.Api.Controllers;

[ApiController]
[Route("api/posts")]
public class PostsController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public PostsController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePostWriteDto request)
    {
        if (request == null) return BadRequest("Body boş olamaz.");
        return await CreatePostInternalAsync(request.Text, request.Tags, request.ReplyToPostId);
    }

    [Authorize]
    [HttpPost("{postId:guid}/replies")]
    public async Task<IActionResult> CreateReply([FromRoute] Guid postId, [FromBody] CreateReplyWriteDto request)
    {
        if (request == null) return BadRequest("Body boş olamaz.");
        return await CreatePostInternalAsync(request.Text, request.Tags, postId);
    }

    [Authorize]
    [HttpPatch("{postId:guid}")]
    public async Task<IActionResult> Update([FromRoute] Guid postId, [FromBody] UpdatePostWriteDto request)
    {
        if (request == null) return BadRequest("Body boş olamaz.");
        if (string.IsNullOrWhiteSpace(request.Text)) return BadRequest("Text zorunlu.");

        string text = request.Text.Trim();
        if (text.Length > 280) return BadRequest("Text en fazla 280 karakter olabilir.");

        if (!TryGetCurrentUserId(User, out Guid currentUserId))
        {
            return Unauthorized("Geçersiz kullanıcı token'ı.");
        }

        Post? post = await _dbContext.Posts.FirstOrDefaultAsync(x => x.Id == postId);
        if (post == null)
        {
            return NotFound("Post bulunamadı.");
        }

        if (post.AuthorId != currentUserId)
        {
            return Forbid();
        }

        post.Text = text;

        if (request.Tags != null)
        {
            List<string> normalizedTags = NormalizeTagNames(request.Tags);
            if (normalizedTags.Count > 10)
            {
                return BadRequest("Bir post en fazla 10 tag içerebilir.");
            }

            await ReplacePostTagsAsync(post.Id, normalizedTags);
        }

        await _dbContext.SaveChangesAsync();
        return Ok(new MessageReadDto
        {
            Message = "Post güncellendi."
        });
    }

    [HttpGet]
    public async Task<IActionResult> GetPosts(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20,
        [FromQuery] Guid? authorId = null,
        [FromQuery] string? tag = null)
    {
        if (!TryValidatePagination(skip, take, out IActionResult? errorResult))
        {
            return errorResult!;
        }

        IQueryable<Post> query = _dbContext.Posts.AsNoTracking();

        if (authorId.HasValue)
        {
            query = query.Where(x => x.AuthorId == authorId.Value);
        }

        if (!string.IsNullOrWhiteSpace(tag))
        {
            string normalizedTag = NormalizeTagName(tag);
            query = query.Where(x => x.PostTags.Any(pt => pt.Tag.Name == normalizedTag));
        }

        List<PostSummaryReadDto> posts = await BuildPostSummaryQuery(query)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync();

        await PopulateTagsAsync(posts);

        return Ok(posts);
    }

    [Authorize]
    [HttpGet("feed")]
    public async Task<IActionResult> GetFeed([FromQuery] int skip = 0, [FromQuery] int take = 20)
    {
        if (!TryValidatePagination(skip, take, out IActionResult? errorResult))
        {
            return errorResult!;
        }

        if (!TryGetCurrentUserId(User, out Guid currentUserId))
        {
            return Unauthorized("Geçersiz kullanıcı token'ı.");
        }

        IQueryable<Guid> followingIdsQuery = _dbContext.Follows
            .AsNoTracking()
            .Where(x => x.FollowerId == currentUserId)
            .Select(x => x.FollowingId);

        IQueryable<Post> feedQuery = _dbContext.Posts.AsNoTracking()
            .Where(x => x.AuthorId == currentUserId || followingIdsQuery.Contains(x.AuthorId));

        List<PostSummaryReadDto> posts = await BuildPostSummaryQuery(feedQuery)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync();

        await PopulateTagsAsync(posts);

        return Ok(posts);
    }

    [HttpGet("{postId:guid}")]
    public async Task<IActionResult> GetById([FromRoute] Guid postId)
    {
        PostSummaryReadDto? post = await BuildPostSummaryQuery(_dbContext.Posts.AsNoTracking().Where(x => x.Id == postId))
            .FirstOrDefaultAsync();

        if (post == null)
        {
            return NotFound("Post bulunamadı.");
        }

        await PopulateTagsAsync(new List<PostSummaryReadDto> { post });

        return Ok(post);
    }

    [HttpGet("{postId:guid}/replies")]
    public async Task<IActionResult> GetReplies([FromRoute] Guid postId, [FromQuery] int skip = 0, [FromQuery] int take = 20)
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

        IQueryable<Post> query = _dbContext.Posts.AsNoTracking().Where(x => x.ReplyToPostId == postId);

        List<PostSummaryReadDto> replies = await BuildPostSummaryQuery(query)
            .OrderBy(x => x.CreatedAtUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync();

        await PopulateTagsAsync(replies);

        return Ok(replies);
    }

    [Authorize]
    [HttpDelete("{postId:guid}")]
    public async Task<IActionResult> Delete([FromRoute] Guid postId)
    {
        if (!TryGetCurrentUserId(User, out Guid currentUserId))
        {
            return Unauthorized("Geçersiz kullanıcı token'ı.");
        }

        Post? post = await _dbContext.Posts.FirstOrDefaultAsync(x => x.Id == postId);
        if (post == null)
        {
            return NotFound("Post bulunamadı.");
        }

        if (post.AuthorId != currentUserId)
        {
            return Forbid();
        }

        _dbContext.Posts.Remove(post);
        await _dbContext.SaveChangesAsync();

        return Ok(new MessageReadDto
        {
            Message = "Post silindi."
        });
    }

    [Authorize]
    [HttpPost("{postId:guid}/like")]
    public async Task<IActionResult> Like([FromRoute] Guid postId)
    {
        if (!TryGetCurrentUserId(User, out Guid currentUserId))
        {
            return Unauthorized("Geçersiz kullanıcı token'ı.");
        }

        bool postExists = await _dbContext.Posts.AnyAsync(x => x.Id == postId);
        if (!postExists)
        {
            return NotFound("Post bulunamadı.");
        }

        PostLike? existingLike = await _dbContext.PostLikes.FindAsync(currentUserId, postId);
        if (existingLike != null)
        {
            return Ok(new MessageReadDto
            {
                Message = "Post zaten beğenilmiş."
            });
        }

        _dbContext.PostLikes.Add(new PostLike
        {
            UserId = currentUserId,
            PostId = postId
        });

        await _dbContext.SaveChangesAsync();
        return Ok(new MessageReadDto
        {
            Message = "Post beğenildi."
        });
    }

    [Authorize]
    [HttpDelete("{postId:guid}/like")]
    public async Task<IActionResult> Unlike([FromRoute] Guid postId)
    {
        if (!TryGetCurrentUserId(User, out Guid currentUserId))
        {
            return Unauthorized("Geçersiz kullanıcı token'ı.");
        }

        PostLike? existingLike = await _dbContext.PostLikes.FindAsync(currentUserId, postId);
        if (existingLike == null)
        {
            return Ok(new MessageReadDto
            {
                Message = "Post daha önce beğenilmemiş."
            });
        }

        _dbContext.PostLikes.Remove(existingLike);
        await _dbContext.SaveChangesAsync();

        return Ok(new MessageReadDto
        {
            Message = "Post beğenisi kaldırıldı."
        });
    }

    [HttpGet("{postId:guid}/likes")]
    public async Task<IActionResult> GetLikes([FromRoute] Guid postId, [FromQuery] int skip = 0, [FromQuery] int take = 20)
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

        int totalCount = await _dbContext.PostLikes.CountAsync(x => x.PostId == postId);

        List<LikeUserReadDto> users = await _dbContext.PostLikes
            .AsNoTracking()
            .Where(x => x.PostId == postId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip(skip)
            .Take(take)
            .Select(x => new LikeUserReadDto
            {
                UserId = x.UserId,
                UserName = x.User.UserName ?? string.Empty,
                LikedAtUtc = x.CreatedAtUtc
            })
            .ToListAsync();

        return Ok(new PostLikesReadDto
        {
            PostId = postId,
            TotalCount = totalCount,
            Items = users
        });
    }

    private async Task<IActionResult> CreatePostInternalAsync(string textInput, IEnumerable<string>? tagsInput, Guid? replyToPostId)
    {
        if (string.IsNullOrWhiteSpace(textInput)) return BadRequest("Text zorunlu.");

        string text = textInput.Trim();
        if (text.Length > 280) return BadRequest("Text en fazla 280 karakter olabilir.");

        if (!TryGetCurrentUserId(User, out Guid currentUserId))
        {
            return Unauthorized("Geçersiz kullanıcı token'ı.");
        }

        if (replyToPostId.HasValue)
        {
            bool replyTargetExists = await _dbContext.Posts.AnyAsync(x => x.Id == replyToPostId.Value);
            if (!replyTargetExists)
            {
                return BadRequest("Yanıtlanacak post bulunamadı.");
            }
        }

        List<string> normalizedTags = NormalizeTagNames(tagsInput);
        if (normalizedTags.Count > 10)
        {
            return BadRequest("Bir post en fazla 10 tag içerebilir.");
        }

        Post post = new Post
        {
            Id = Guid.NewGuid(),
            AuthorId = currentUserId,
            Text = text,
            ReplyToPostId = replyToPostId
        };

        _dbContext.Posts.Add(post);
        await ReplacePostTagsAsync(post.Id, normalizedTags);

        await _dbContext.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { postId = post.Id }, new CreatedPostReadDto
        {
            Message = "Post oluşturuldu.",
            PostId = post.Id
        });
    }

    private async Task ReplacePostTagsAsync(Guid postId, List<string> normalizedTags)
    {
        List<PostTag> existingRelations = await _dbContext.PostTags
            .Where(x => x.PostId == postId)
            .ToListAsync();

        if (existingRelations.Count > 0)
        {
            _dbContext.PostTags.RemoveRange(existingRelations);
        }

        if (normalizedTags.Count == 0)
        {
            return;
        }

        List<Tag> existingTags = await _dbContext.Tags
            .Where(x => normalizedTags.Contains(x.Name))
            .ToListAsync();

        Dictionary<string, Tag> tagsByName = existingTags
            .ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);

        foreach (string tagName in normalizedTags)
        {
            if (!tagsByName.TryGetValue(tagName, out Tag? tag))
            {
                tag = new Tag
                {
                    Id = Guid.NewGuid(),
                    Name = tagName
                };

                _dbContext.Tags.Add(tag);
                tagsByName[tagName] = tag;
            }

            _dbContext.PostTags.Add(new PostTag
            {
                PostId = postId,
                TagId = tag.Id
            });
        }
    }

    private IQueryable<PostSummaryReadDto> BuildPostSummaryQuery(IQueryable<Post> query)
    {
        return query.Select(x => new PostSummaryReadDto
        {
            Id = x.Id,
            AuthorId = x.AuthorId,
            AuthorUserName = x.Author.UserName ?? string.Empty,
            Text = x.Text,
            ReplyToPostId = x.ReplyToPostId,
            CreatedAtUtc = x.CreatedAtUtc,
            LikeCount = x.Likes.Count,
            CommentCount = x.Comments.Count,
            ReplyCount = x.Replies.Count
        });
    }

    private async Task PopulateTagsAsync(List<PostSummaryReadDto> posts)
    {
        if (posts.Count == 0)
        {
            return;
        }

        List<Guid> postIds = posts.Select(x => x.Id).ToList();

        var tagRows = await _dbContext.PostTags
            .AsNoTracking()
            .Where(x => postIds.Contains(x.PostId))
            .Select(x => new { x.PostId, TagName = x.Tag.Name })
            .ToListAsync();

        Dictionary<Guid, List<string>> tagsByPostId = tagRows
            .GroupBy(x => x.PostId)
            .ToDictionary(
                x => x.Key,
                x => x.Select(t => t.TagName).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(t => t).ToList());

        foreach (PostSummaryReadDto post in posts)
        {
            if (tagsByPostId.TryGetValue(post.Id, out List<string>? tags))
            {
                post.Tags = tags;
            }
        }
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

    private static List<string> NormalizeTagNames(IEnumerable<string>? tags)
    {
        HashSet<string> normalized = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (tags == null)
        {
            return normalized.ToList();
        }

        foreach (string? rawTag in tags)
        {
            if (string.IsNullOrWhiteSpace(rawTag))
            {
                continue;
            }

            string tag = NormalizeTagName(rawTag);
            if (tag.Length == 0)
            {
                continue;
            }

            if (tag.Length > 50)
            {
                tag = tag[..50];
            }

            normalized.Add(tag);
        }

        return normalized.ToList();
    }

    private static string NormalizeTagName(string rawTag)
    {
        string tag = rawTag.Trim();
        while (tag.StartsWith('#'))
        {
            tag = tag[1..];
        }

        return tag.Trim().ToLowerInvariant();
    }

    private static bool TryGetCurrentUserId(ClaimsPrincipal user, out Guid userId)
    {
        userId = Guid.Empty;

        string? userIdValue = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdValue, out userId);
    }
}
