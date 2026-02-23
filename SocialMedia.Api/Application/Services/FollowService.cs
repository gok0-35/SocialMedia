using Microsoft.EntityFrameworkCore;
using SocialMedia.Api.Application.Dtos.Common;
using SocialMedia.Api.Application.Dtos.Follows;
using SocialMedia.Api.Application.Services.Abstractions;
using SocialMedia.Api.Domain.Entities;
using SocialMedia.Api.Infrastructure.Persistence;

namespace SocialMedia.Api.Application.Services;

public class FollowService : IFollowService
{
    private readonly AppDbContext _dbContext;

    public FollowService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ServiceResult<MessageReadDto>> FollowAsync(Guid currentUserId, Guid followingUserId)
    {
        if (followingUserId == currentUserId)
        {
            return ServiceResult<MessageReadDto>.Fail(ServiceErrorType.BadRequest, "Kendini takip edemezsin.");
        }

        bool followingUserExists = await _dbContext.Users.AnyAsync(x => x.Id == followingUserId);
        if (!followingUserExists)
        {
            return ServiceResult<MessageReadDto>.Fail(ServiceErrorType.NotFound, "Takip edilecek kullanıcı bulunamadı.");
        }

        Follow? existingFollow = await _dbContext.Follows.FindAsync(currentUserId, followingUserId);
        if (existingFollow != null)
        {
            return ServiceResult<MessageReadDto>.Success(new MessageReadDto
            {
                Message = "Kullanıcı zaten takip ediliyor."
            });
        }

        _dbContext.Follows.Add(new Follow
        {
            FollowerId = currentUserId,
            FollowingId = followingUserId
        });

        await _dbContext.SaveChangesAsync();

        return ServiceResult<MessageReadDto>.Success(new MessageReadDto
        {
            Message = "Kullanıcı takip edildi."
        });
    }

    public async Task<ServiceResult<MessageReadDto>> UnfollowAsync(Guid currentUserId, Guid followingUserId)
    {
        Follow? existingFollow = await _dbContext.Follows.FindAsync(currentUserId, followingUserId);
        if (existingFollow == null)
        {
            return ServiceResult<MessageReadDto>.Success(new MessageReadDto
            {
                Message = "Kullanıcı zaten takip edilmiyor."
            });
        }

        _dbContext.Follows.Remove(existingFollow);
        await _dbContext.SaveChangesAsync();

        return ServiceResult<MessageReadDto>.Success(new MessageReadDto
        {
            Message = "Takip bırakıldı."
        });
    }

    public async Task<ServiceResult<FollowListReadDto>> GetFollowersAsync(Guid userId, int skip, int take)
    {
        ServiceError? paginationError = ValidatePagination(skip, take);
        if (paginationError != null)
        {
            return ServiceResult<FollowListReadDto>.Fail(paginationError.Type, paginationError.Message);
        }

        bool userExists = await _dbContext.Users.AnyAsync(x => x.Id == userId);
        if (!userExists)
        {
            return ServiceResult<FollowListReadDto>.Fail(ServiceErrorType.NotFound, "Kullanıcı bulunamadı.");
        }

        int totalCount = await _dbContext.Follows.CountAsync(x => x.FollowingId == userId);

        List<FollowUserReadDto> followers = await _dbContext.Follows
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

        return ServiceResult<FollowListReadDto>.Success(new FollowListReadDto
        {
            UserId = userId,
            TotalCount = totalCount,
            Items = followers
        });
    }

    public async Task<ServiceResult<FollowListReadDto>> GetFollowingAsync(Guid userId, int skip, int take)
    {
        ServiceError? paginationError = ValidatePagination(skip, take);
        if (paginationError != null)
        {
            return ServiceResult<FollowListReadDto>.Fail(paginationError.Type, paginationError.Message);
        }

        bool userExists = await _dbContext.Users.AnyAsync(x => x.Id == userId);
        if (!userExists)
        {
            return ServiceResult<FollowListReadDto>.Fail(ServiceErrorType.NotFound, "Kullanıcı bulunamadı.");
        }

        int totalCount = await _dbContext.Follows.CountAsync(x => x.FollowerId == userId);

        List<FollowUserReadDto> following = await _dbContext.Follows
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

        return ServiceResult<FollowListReadDto>.Success(new FollowListReadDto
        {
            UserId = userId,
            TotalCount = totalCount,
            Items = following
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
}

