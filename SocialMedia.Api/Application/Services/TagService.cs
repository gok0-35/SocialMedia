using SocialMedia.Api.Application.Dtos.Tags;
using SocialMedia.Api.Application.Repositories.Abstractions;
using SocialMedia.Api.Application.Services.Abstractions;
using SocialMedia.Api.Domain.Entities;

namespace SocialMedia.Api.Application.Services;

public class TagService : ITagService
{
    private readonly ITagRepository _tagRepository;

    public TagService(ITagRepository tagRepository)
    {
        _tagRepository = tagRepository;
    }

    public async Task<ServiceResult<List<TagSummaryReadDto>>> GetTagsAsync(int skip, int take, string? q)
    {
        ServiceError? paginationError = ValidatePagination(skip, take);
        if (paginationError != null)
        {
            return ServiceResult<List<TagSummaryReadDto>>.Fail(paginationError.Type, paginationError.Message);
        }

        string? normalizedQuery = null;
        if (!string.IsNullOrWhiteSpace(q))
        {
            normalizedQuery = NormalizeTagName(q);
        }

        List<TagSummaryReadDto> tags = await _tagRepository.GetTagsAsync(skip, take, normalizedQuery);

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

        List<TrendingTagReadDto> trending = await _tagRepository.GetTrendingAsync(take, fromDate);

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

        Tag? tag = await _tagRepository.GetByNameAsync(normalizedTagName);

        if (tag == null)
        {
            return ServiceResult<TagPostsReadDto>.Fail(ServiceErrorType.NotFound, "Tag bulunamadı.");
        }

        List<TagPostReadDto> posts = await _tagRepository.GetPostsByTagIdAsync(tag.Id, skip, take);

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
