using SocialMedia.Api.Application.Dtos.Common;
using SocialMedia.Api.Application.Dtos.Follows;
using SocialMedia.Api.Application.Repositories.Abstractions;
using SocialMedia.Api.Application.Services.Abstractions;
using SocialMedia.Api.Domain.Entities;

namespace SocialMedia.Api.Application.Services;

public class FollowService : IFollowService
{
    private readonly IFollowRepository _followRepository;

    public FollowService(IFollowRepository followRepository)
    {
        _followRepository = followRepository;
    }

    public async Task<ServiceResult<MessageReadDto>> FollowAsync(Guid currentUserId, Guid followingUserId)
    {
        if (followingUserId == currentUserId)
        {
            return ServiceResult<MessageReadDto>.Fail(ServiceErrorType.BadRequest, "Kendini takip edemezsin.");
        }

        bool followingUserExists = await _followRepository.UserExistsAsync(followingUserId);
        if (!followingUserExists)
        {
            return ServiceResult<MessageReadDto>.Fail(ServiceErrorType.NotFound, "Takip edilecek kullanıcı bulunamadı.");
        }

        Follow? existingFollow = await _followRepository.GetByIdAsync(currentUserId, followingUserId);
        if (existingFollow != null)
        {
            return ServiceResult<MessageReadDto>.Success(new MessageReadDto
            {
                Message = "Kullanıcı zaten takip ediliyor."
            });
        }

        await _followRepository.AddAsync(new Follow
        {
            FollowerId = currentUserId,
            FollowingId = followingUserId
        });

        await _followRepository.SaveChangesAsync();

        return ServiceResult<MessageReadDto>.Success(new MessageReadDto
        {
            Message = "Kullanıcı takip edildi."
        });
    }

    public async Task<ServiceResult<MessageReadDto>> UnfollowAsync(Guid currentUserId, Guid followingUserId)
    {
        Follow? existingFollow = await _followRepository.GetByIdAsync(currentUserId, followingUserId);
        if (existingFollow == null)
        {
            return ServiceResult<MessageReadDto>.Success(new MessageReadDto
            {
                Message = "Kullanıcı zaten takip edilmiyor."
            });
        }

        _followRepository.Remove(existingFollow);
        await _followRepository.SaveChangesAsync();

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

        bool userExists = await _followRepository.UserExistsAsync(userId);
        if (!userExists)
        {
            return ServiceResult<FollowListReadDto>.Fail(ServiceErrorType.NotFound, "Kullanıcı bulunamadı.");
        }

        int totalCount = await _followRepository.CountFollowersAsync(userId);

        List<FollowUserReadDto> followers = await _followRepository.GetFollowersAsync(userId, skip, take);

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

        bool userExists = await _followRepository.UserExistsAsync(userId);
        if (!userExists)
        {
            return ServiceResult<FollowListReadDto>.Fail(ServiceErrorType.NotFound, "Kullanıcı bulunamadı.");
        }

        int totalCount = await _followRepository.CountFollowingAsync(userId);

        List<FollowUserReadDto> following = await _followRepository.GetFollowingAsync(userId, skip, take);

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
