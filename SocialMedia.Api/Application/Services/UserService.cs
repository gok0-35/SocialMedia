using SocialMedia.Api.Application.Dtos.Common;
using SocialMedia.Api.Application.Dtos.Users;
using SocialMedia.Api.Application.Repositories.Abstractions;
using SocialMedia.Api.Application.Services.Abstractions;
using SocialMedia.Api.Domain.Entities;

namespace SocialMedia.Api.Application.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;

    public UserService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<ServiceResult<UserProfileReadDto>> GetByIdAsync(Guid userId)
    {
        UserProfileReadDto? user = await _userRepository.GetProfileAsync(userId);

        if (user == null)
        {
            return ServiceResult<UserProfileReadDto>.Fail(ServiceErrorType.NotFound, "Kullanıcı bulunamadı.");
        }

        return ServiceResult<UserProfileReadDto>.Success(user);
    }

    public async Task<ServiceResult<MyProfileReadDto>> GetMeAsync(Guid currentUserId)
    {
        MyProfileReadDto? me = await _userRepository.GetMyProfileAsync(currentUserId);

        if (me == null)
        {
            return ServiceResult<MyProfileReadDto>.Fail(ServiceErrorType.NotFound, "Kullanıcı bulunamadı.");
        }

        return ServiceResult<MyProfileReadDto>.Success(me);
    }

    public async Task<ServiceResult<MessageReadDto>> UpdateMeAsync(Guid currentUserId, UpdateMyProfileWriteDto request)
    {
        if (request == null)
        {
            return ServiceResult<MessageReadDto>.Fail(ServiceErrorType.BadRequest, "Body boş olamaz.");
        }

        ApplicationUser? user = await _userRepository.GetByIdAsync(currentUserId);
        if (user == null)
        {
            return ServiceResult<MessageReadDto>.Fail(ServiceErrorType.NotFound, "Kullanıcı bulunamadı.");
        }

        if (request.Bio != null)
        {
            string bio = request.Bio.Trim();
            if (bio.Length > 500)
            {
                return ServiceResult<MessageReadDto>.Fail(ServiceErrorType.BadRequest, "Bio en fazla 500 karakter olabilir.");
            }

            user.Bio = bio.Length == 0 ? null : bio;
        }

        if (request.AvatarUrl != null)
        {
            string avatarUrl = request.AvatarUrl.Trim();
            if (avatarUrl.Length > 1000)
            {
                return ServiceResult<MessageReadDto>.Fail(ServiceErrorType.BadRequest, "AvatarUrl en fazla 1000 karakter olabilir.");
            }

            if (avatarUrl.Length == 0)
            {
                user.AvatarUrl = null;
            }
            else if (!Uri.IsWellFormedUriString(avatarUrl, UriKind.Absolute))
            {
                return ServiceResult<MessageReadDto>.Fail(ServiceErrorType.BadRequest, "AvatarUrl geçerli bir URL olmalı.");
            }
            else
            {
                user.AvatarUrl = avatarUrl;
            }
        }

        await _userRepository.SaveChangesAsync();

        return ServiceResult<MessageReadDto>.Success(new MessageReadDto
        {
            Message = "Profil güncellendi."
        });
    }

    public async Task<ServiceResult<List<UserPostReadDto>>> GetUserPostsAsync(Guid userId, int skip, int take)
    {
        ServiceError? paginationError = ValidatePagination(skip, take);
        if (paginationError != null)
        {
            return ServiceResult<List<UserPostReadDto>>.Fail(paginationError.Type, paginationError.Message);
        }

        bool userExists = await _userRepository.ExistsAsync(userId);
        if (!userExists)
        {
            return ServiceResult<List<UserPostReadDto>>.Fail(ServiceErrorType.NotFound, "Kullanıcı bulunamadı.");
        }

        List<UserPostReadDto> posts = await _userRepository.GetPostsAsync(userId, skip, take);

        return ServiceResult<List<UserPostReadDto>>.Success(posts);
    }

    public async Task<ServiceResult<List<UserCommentReadDto>>> GetUserCommentsAsync(Guid userId, int skip, int take)
    {
        ServiceError? paginationError = ValidatePagination(skip, take);
        if (paginationError != null)
        {
            return ServiceResult<List<UserCommentReadDto>>.Fail(paginationError.Type, paginationError.Message);
        }

        bool userExists = await _userRepository.ExistsAsync(userId);
        if (!userExists)
        {
            return ServiceResult<List<UserCommentReadDto>>.Fail(ServiceErrorType.NotFound, "Kullanıcı bulunamadı.");
        }

        List<UserCommentReadDto> comments = await _userRepository.GetCommentsAsync(userId, skip, take);

        return ServiceResult<List<UserCommentReadDto>>.Success(comments);
    }

    public async Task<ServiceResult<List<UserLikedPostReadDto>>> GetLikedPostsAsync(Guid userId, int skip, int take)
    {
        ServiceError? paginationError = ValidatePagination(skip, take);
        if (paginationError != null)
        {
            return ServiceResult<List<UserLikedPostReadDto>>.Fail(paginationError.Type, paginationError.Message);
        }

        bool userExists = await _userRepository.ExistsAsync(userId);
        if (!userExists)
        {
            return ServiceResult<List<UserLikedPostReadDto>>.Fail(ServiceErrorType.NotFound, "Kullanıcı bulunamadı.");
        }

        List<UserLikedPostReadDto> likedPosts = await _userRepository.GetLikedPostsAsync(userId, skip, take);

        return ServiceResult<List<UserLikedPostReadDto>>.Success(likedPosts);
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
