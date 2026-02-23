using Microsoft.EntityFrameworkCore;
using SocialMedia.Api.Application.Dtos.Follows;
using SocialMedia.Api.Application.Repositories.Abstractions;
using SocialMedia.Api.Domain.Entities;

namespace SocialMedia.Api.Infrastructure.Persistence.Repositories;

public class FollowRepository : IFollowRepository
{
    private readonly AppDbContext _dbContext;

    public FollowRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> UserExistsAsync(Guid userId)
    {
        return _dbContext.Users.AnyAsync(x => x.Id == userId);
    }

    public Task<Follow?> GetByIdAsync(Guid followerId, Guid followingId)
    {
        return _dbContext.Follows.FindAsync(followerId, followingId).AsTask();
    }

    public Task AddAsync(Follow follow)
    {
        _dbContext.Follows.Add(follow);
        return Task.CompletedTask;
    }

    public void Remove(Follow follow)
    {
        _dbContext.Follows.Remove(follow);
    }

    public Task<int> CountFollowersAsync(Guid userId)
    {
        return _dbContext.Follows.CountAsync(x => x.FollowingId == userId);
    }

    public Task<int> CountFollowingAsync(Guid userId)
    {
        return _dbContext.Follows.CountAsync(x => x.FollowerId == userId);
    }

    public Task<List<FollowUserReadDto>> GetFollowersAsync(Guid userId, int skip, int take)
    {
        return _dbContext.Follows
            .AsNoTracking()
            .Where(x => x.FollowingId == userId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip(skip)
            .Take(take)
            .Select(x => new FollowUserReadDto
            {
                UserId = x.FollowerId,
                UserName = x.Follower.UserName ?? string.Empty,
                FollowedAtUtc = x.CreatedAtUtc
            })
            .ToListAsync();
    }

    public Task<List<FollowUserReadDto>> GetFollowingAsync(Guid userId, int skip, int take)
    {
        return _dbContext.Follows
            .AsNoTracking()
            .Where(x => x.FollowerId == userId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip(skip)
            .Take(take)
            .Select(x => new FollowUserReadDto
            {
                UserId = x.FollowingId,
                UserName = x.Following.UserName ?? string.Empty,
                FollowedAtUtc = x.CreatedAtUtc
            })
            .ToListAsync();
    }

    public Task SaveChangesAsync()
    {
        return _dbContext.SaveChangesAsync();
    }
}
