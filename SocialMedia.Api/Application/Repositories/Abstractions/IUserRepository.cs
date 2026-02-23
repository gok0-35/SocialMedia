using SocialMedia.Api.Application.Dtos.Users;
using SocialMedia.Api.Domain.Entities;

namespace SocialMedia.Api.Application.Repositories.Abstractions;

public interface IUserRepository
{
    Task<UserProfileReadDto?> GetProfileAsync(Guid userId);
    Task<MyProfileReadDto?> GetMyProfileAsync(Guid currentUserId);
    Task<ApplicationUser?> GetByIdAsync(Guid userId);
    Task<bool> ExistsAsync(Guid userId);

    Task<List<UserPostReadDto>> GetPostsAsync(Guid userId, int skip, int take);
    Task<List<UserCommentReadDto>> GetCommentsAsync(Guid userId, int skip, int take);
    Task<List<UserLikedPostReadDto>> GetLikedPostsAsync(Guid userId, int skip, int take);

    Task SaveChangesAsync();
}
