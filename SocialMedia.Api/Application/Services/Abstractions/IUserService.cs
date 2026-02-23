using SocialMedia.Api.Application.Dtos.Common;
using SocialMedia.Api.Application.Dtos.Users;

namespace SocialMedia.Api.Application.Services.Abstractions;

public interface IUserService
{
    Task<ServiceResult<UserProfileReadDto>> GetByIdAsync(Guid userId);
    Task<ServiceResult<MyProfileReadDto>> GetMeAsync(Guid currentUserId);
    Task<ServiceResult<MessageReadDto>> UpdateMeAsync(Guid currentUserId, UpdateMyProfileWriteDto request);
    Task<ServiceResult<List<UserPostReadDto>>> GetUserPostsAsync(Guid userId, int skip, int take);
    Task<ServiceResult<List<UserCommentReadDto>>> GetUserCommentsAsync(Guid userId, int skip, int take);
    Task<ServiceResult<List<UserLikedPostReadDto>>> GetLikedPostsAsync(Guid userId, int skip, int take);
}

