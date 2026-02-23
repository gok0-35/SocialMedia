using Microsoft.EntityFrameworkCore;
using SocialMedia.Api.Application.Dtos.Tags;
using SocialMedia.Api.Application.Services.Abstractions;
using SocialMedia.Api.Infrastructure.Persistence;

namespace SocialMedia.Api.Application.Services;

public class TagService : ITagService
{
    private readonly AppDbContext _dbContext;

    public TagService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ServiceResult<List<TagSummaryReadDto>>> GetTagsAsync(int skip, int take, string? q)
    {
        ServiceError? paginationError = ValidatePagination(skip, take);
        if (paginationError != null)
        {
            return ServiceResult<List<TagSummaryReadDto>>.Fail(paginationError.Type, paginationError.Message);
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

        return ServiceResult<List<TagSummaryReadDto>>.Success(tags);
    }

    public async Task<ServiceResult<List<TrendingTagReadDto>>> GetTrendingAsync(int take, int days)
    {
        if (take <= 0 || take > 100)
        {
            return ServiceResult<List<TrendingTagReadDto>>.Fail(ServiceErrorType.BadRequest, "take 1 ile 100 arasında olmalı.");
        }

        if (days <= 0 || days > 365)
        {
            return ServiceResult<List<TrendingTagReadDto>>.Fail(ServiceErrorType.BadRequest, "days 1 ile 365 arasında olmalı.");
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

        return ServiceResult<List<TrendingTagReadDto>>.Success(trending);
    }

    public async Task<ServiceResult<TagPostsReadDto>> GetPostsByTagAsync(string tagName, int skip, int take)
    {
        ServiceError? paginationError = ValidatePagination(skip, take);
        if (paginationError != null)
        {
            return ServiceResult<TagPostsReadDto>.Fail(paginationError.Type, paginationError.Message);
        }

        string normalizedTagName = NormalizeTagName(tagName);
        if (string.IsNullOrWhiteSpace(normalizedTagName))
        {
            return ServiceResult<TagPostsReadDto>.Fail(ServiceErrorType.BadRequest, "Geçerli bir tag adı girilmelidir.");
        }

        Domain.Entities.Tag? tag = await _dbContext.Tags
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Name == normalizedTagName);

        if (tag == null)
        {
            return ServiceResult<TagPostsReadDto>.Fail(ServiceErrorType.NotFound, "Tag bulunamadı.");
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

        return ServiceResult<TagPostsReadDto>.Success(new TagPostsReadDto
        {
            Tag = tag.Name,
            Items = posts
        });
    }

    private static ServiceError? ValidatePagination(int skip, int take)
    {
        if (skip < 0)
        {
            return new ServiceError(ServiceErrorType.BadRequest, "skip 0 veya daha büyük olmalı.");
        }

        if (take <= 0 || take > 100)
        {
            return new ServiceError(ServiceErrorType.BadRequest, "take 1 ile 100 arasında olmalı.");
        }

        return null;
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

