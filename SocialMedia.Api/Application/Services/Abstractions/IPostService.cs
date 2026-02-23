using SocialMedia.Api.Application.Dtos.Common;
using SocialMedia.Api.Application.Dtos.Posts;

namespace SocialMedia.Api.Application.Services.Abstractions;

public interface IPostService
{
    Task<ServiceResult<CreatedPostReadDto>> CreateAsync(Guid currentUserId, CreatePostWriteDto request);
    Task<ServiceResult<CreatedPostReadDto>> CreateReplyAsync(Guid currentUserId, Guid postId, CreateReplyWriteDto request);
    Task<ServiceResult<MessageReadDto>> UpdateAsync(Guid currentUserId, Guid postId, UpdatePostWriteDto request);
    Task<ServiceResult<List<PostSummaryReadDto>>> GetPostsAsync(int skip, int take, Guid? authorId, string? tag);
    Task<ServiceResult<List<PostSummaryReadDto>>> GetFeedAsync(Guid currentUserId, int skip, int take);
    Task<ServiceResult<PostSummaryReadDto>> GetByIdAsync(Guid postId);
    Task<ServiceResult<List<PostSummaryReadDto>>> GetRepliesAsync(Guid postId, int skip, int take);
    Task<ServiceResult<MessageReadDto>> DeleteAsync(Guid currentUserId, Guid postId);
    Task<ServiceResult<MessageReadDto>> LikeAsync(Guid currentUserId, Guid postId);
    Task<ServiceResult<MessageReadDto>> UnlikeAsync(Guid currentUserId, Guid postId);
    Task<ServiceResult<PostLikesReadDto>> GetLikesAsync(Guid postId, int skip, int take);
}

