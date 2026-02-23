using SocialMedia.Api.Application.Dtos.Common;
using SocialMedia.Api.Application.Dtos.Follows;

namespace SocialMedia.Api.Application.Services.Abstractions;

public interface IFollowService
{
    Task<ServiceResult<MessageReadDto>> FollowAsync(Guid currentUserId, Guid followingUserId);
    Task<ServiceResult<MessageReadDto>> UnfollowAsync(Guid currentUserId, Guid followingUserId);
    Task<ServiceResult<FollowListReadDto>> GetFollowersAsync(Guid userId, int skip, int take);
    Task<ServiceResult<FollowListReadDto>> GetFollowingAsync(Guid userId, int skip, int take);
}

