using SocialMedia.Api.Application.Dtos.Tags;

namespace SocialMedia.Api.Application.Services.Abstractions;

public interface ITagService
{
    Task<ServiceResult<List<TagSummaryReadDto>>> GetTagsAsync(int skip, int take, string? q);
    Task<ServiceResult<List<TrendingTagReadDto>>> GetTrendingAsync(int take, int days);
    Task<ServiceResult<TagPostsReadDto>> GetPostsByTagAsync(string tagName, int skip, int take);
}

