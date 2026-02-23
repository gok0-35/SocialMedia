using SocialMedia.Api.Application.Dtos.Follows;
using SocialMedia.Api.Domain.Entities;

namespace SocialMedia.Api.Application.Repositories.Abstractions;

public interface IFollowRepository
{
    Task<bool> UserExistsAsync(Guid userId);
    Task<Follow?> GetByIdAsync(Guid followerId, Guid followingId);
    Task AddAsync(Follow follow);
    void Remove(Follow follow);

    Task<int> CountFollowersAsync(Guid userId);
    Task<int> CountFollowingAsync(Guid userId);
    Task<List<FollowUserReadDto>> GetFollowersAsync(Guid userId, int skip, int take);
    Task<List<FollowUserReadDto>> GetFollowingAsync(Guid userId, int skip, int take);

    Task SaveChangesAsync();
}
