using SocialMedia.Api.Application.Dtos.Posts;
using SocialMedia.Api.Domain.Entities;

namespace SocialMedia.Api.Application.Repositories.Abstractions;

public interface IPostRepository
{
    Task<Post?> GetByIdAsync(Guid postId);
    Task<bool> ExistsAsync(Guid postId);
    Task AddAsync(Post post);
    void Remove(Post post);

    Task<PostLike?> GetLikeAsync(Guid userId, Guid postId);
    Task AddLikeAsync(PostLike like);
    void RemoveLike(PostLike like);

    Task<int> CountLikesAsync(Guid postId);
    Task<List<LikeUserReadDto>> GetLikesAsync(Guid postId, int skip, int take);

    Task<List<PostSummaryReadDto>> GetPostsAsync(int skip, int take, Guid? authorId, string? normalizedTag);
    Task<List<PostSummaryReadDto>> GetFeedAsync(Guid currentUserId, int skip, int take);
    Task<PostSummaryReadDto?> GetSummaryByIdAsync(Guid postId);
    Task<List<PostSummaryReadDto>> GetRepliesAsync(Guid postId, int skip, int take);

    Task ReplacePostTagsAsync(Guid postId, List<string> normalizedTags);
    Task SaveChangesAsync();
}
