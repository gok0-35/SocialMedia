using Microsoft.EntityFrameworkCore;
using SocialMedia.Api.Application.Dtos.Tags;
using SocialMedia.Api.Application.Repositories.Abstractions;
using SocialMedia.Api.Domain.Entities;

namespace SocialMedia.Api.Infrastructure.Persistence.Repositories;

public class TagRepository : ITagRepository
{
    private readonly AppDbContext _dbContext;

    public TagRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<List<TagSummaryReadDto>> GetTagsAsync(int skip, int take, string? normalizedQuery)
    {
        IQueryable<Tag> query = _dbContext.Tags.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(normalizedQuery))
        {
            query = query.Where(x => x.Name.Contains(normalizedQuery));
        }

        return query
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
    }

    public Task<List<TrendingTagReadDto>> GetTrendingAsync(int take, DateTimeOffset fromDate)
    {
        return _dbContext.PostTags
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
    }

    public Task<Tag?> GetByNameAsync(string normalizedTagName)
    {
        return _dbContext.Tags
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Name == normalizedTagName);
    }

    public Task<List<TagPostReadDto>> GetPostsByTagIdAsync(Guid tagId, int skip, int take)
    {
        return _dbContext.Posts
            .AsNoTracking()
            .Where(x => x.PostTags.Any(pt => pt.TagId == tagId))
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
    }
}
