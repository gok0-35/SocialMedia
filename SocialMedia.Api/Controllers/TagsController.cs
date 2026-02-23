using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SocialMedia.Api.Application.Dtos.Tags;
using SocialMedia.Api.Infrastructure.Persistence;

namespace SocialMedia.Api.Controllers;

[ApiController]
[Route("api/tags")]
public class TagsController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public TagsController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> GetTags([FromQuery] int skip = 0, [FromQuery] int take = 20, [FromQuery] string? q = null)
    {
        if (!TryValidatePagination(skip, take, out IActionResult? errorResult))
        {
            return errorResult!;
        }

        IQueryable<Domain.Entities.Tag> query = _dbContext.Tags.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(q))
        {
            string normalizedQuery = NormalizeTagName(q);
            query = query.Where(x => x.Name.Contains(normalizedQuery));
        }

        List<TagSummaryReadDto> tags = await query
            .OrderBy(x => x.Name)
            .Skip(skip)
            .Take(take)
            .Select(x => new TagSummaryReadDto
            {
                Id = x.Id,
                Name = x.Name,
                CreatedAtUtc = x.CreatedAtUtc,
                PostCount = x.PostTags.Count
            })
            .ToListAsync();

        return Ok(tags);
    }

    [HttpGet("trending")]
    public async Task<IActionResult> GetTrending([FromQuery] int take = 10, [FromQuery] int days = 7)
    {
        if (take <= 0 || take > 100)
        {
            return BadRequest("take 1 ile 100 arasında olmalı.");
        }

        if (days <= 0 || days > 365)
        {
            return BadRequest("days 1 ile 365 arasında olmalı.");
        }

        DateTimeOffset fromDate = DateTimeOffset.UtcNow.AddDays(-days);

        List<TrendingTagReadDto> trending = await _dbContext.PostTags
            .AsNoTracking()
            .Where(x => x.Post.CreatedAtUtc >= fromDate)
            .GroupBy(x => new { x.TagId, x.Tag.Name })
            .Select(x => new TrendingTagReadDto
            {
                TagId = x.Key.TagId,
                Name = x.Key.Name,
                PostCount = x.Count()
            })
            .OrderByDescending(x => x.PostCount)
            .ThenBy(x => x.Name)
            .Take(take)
            .ToListAsync();

        return Ok(trending);
    }

    [HttpGet("{tagName}/posts")]
    public async Task<IActionResult> GetPostsByTag([FromRoute] string tagName, [FromQuery] int skip = 0, [FromQuery] int take = 20)
    {
        if (!TryValidatePagination(skip, take, out IActionResult? errorResult))
        {
            return errorResult!;
        }

        string normalizedTagName = NormalizeTagName(tagName);
        if (string.IsNullOrWhiteSpace(normalizedTagName))
        {
            return BadRequest("Geçerli bir tag adı girilmelidir.");
        }

        Domain.Entities.Tag? tag = await _dbContext.Tags
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Name == normalizedTagName);

        if (tag == null)
        {
            return NotFound("Tag bulunamadı.");
        }

        List<TagPostReadDto> posts = await _dbContext.Posts
            .AsNoTracking()
            .Where(x => x.PostTags.Any(pt => pt.TagId == tag.Id))
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip(skip)
            .Take(take)
            .Select(x => new TagPostReadDto
            {
                Id = x.Id,
                AuthorId = x.AuthorId,
                AuthorUserName = x.Author.UserName ?? string.Empty,
                Text = x.Text,
                CreatedAtUtc = x.CreatedAtUtc,
                ReplyToPostId = x.ReplyToPostId,
                LikeCount = x.Likes.Count,
                CommentCount = x.Comments.Count
            })
            .ToListAsync();

        return Ok(new TagPostsReadDto
        {
            Tag = tag.Name,
            Items = posts
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

    private static string NormalizeTagName(string rawTag)
    {
        string tag = rawTag.Trim();
        while (tag.StartsWith('#'))
        {
            tag = tag[1..];
        }

        return tag.Trim().ToLowerInvariant();
    }
}
