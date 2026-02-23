using SocialMedia.Api.Application.Dtos.Tags;
using SocialMedia.Api.Domain.Entities;

namespace SocialMedia.Api.Application.Repositories.Abstractions;

public interface ITagRepository
{
    Task<List<TagSummaryReadDto>> GetTagsAsync(int skip, int take, string? normalizedQuery);
    Task<List<TrendingTagReadDto>> GetTrendingAsync(int take, DateTimeOffset fromDate);
    Task<Tag?> GetByNameAsync(string normalizedTagName);
    Task<List<TagPostReadDto>> GetPostsByTagIdAsync(Guid tagId, int skip, int take);
}
