using Microsoft.EntityFrameworkCore;
using SocialMedia.Api.Application.Dtos.Common;
using SocialMedia.Api.Application.Dtos.Users;
using SocialMedia.Api.Application.Services.Abstractions;
using SocialMedia.Api.Domain.Entities;
using SocialMedia.Api.Infrastructure.Persistence;

namespace SocialMedia.Api.Application.Services;

public class UserService : IUserService
{
    private readonly AppDbContext _dbContext;

    public UserService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ServiceResult<UserProfileReadDto>> GetByIdAsync(Guid userId)
    {
        UserProfileReadDto? user = await _dbContext.Users
            .AsNoTracking()
            .Where(x => x.Id == userId)
            .Select(x => new UserProfileReadDto
            {
                Id = x.Id,
                UserName = x.UserName ?? string.Empty,
                Bio = x.Bio,
                AvatarUrl = x.AvatarUrl,
                CreatedAtUtc = x.CreatedAtUtc,
                PostCount = x.Posts.Count,
                CommentCount = x.Comments.Count,
                LikeCount = x.Likes.Count,
                FollowersCount = x.Followers.Count,
                FollowingCount = x.Following.Count
            })
            .FirstOrDefaultAsync();

        if (user == null)
        {
            return ServiceResult<UserProfileReadDto>.Fail(ServiceErrorType.NotFound, "Kullanıcı bulunamadı.");
        }

        return ServiceResult<UserProfileReadDto>.Success(user);
    }

    public async Task<ServiceResult<MyProfileReadDto>> GetMeAsync(Guid currentUserId)
    {
        MyProfileReadDto? me = await _dbContext.Users
            .AsNoTracking()
            .Where(x => x.Id == currentUserId)
            .Select(x => new MyProfileReadDto
            {
                Id = x.Id,
                UserName = x.UserName ?? string.Empty,
                Email = x.Email ?? string.Empty,
                Bio = x.Bio,
                AvatarUrl = x.AvatarUrl,
                CreatedAtUtc = x.CreatedAtUtc,
                PostCount = x.Posts.Count,
                CommentCount = x.Comments.Count,
                LikeCount = x.Likes.Count,
                FollowersCount = x.Followers.Count,
                FollowingCount = x.Following.Count
            })
            .FirstOrDefaultAsync();

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

        ApplicationUser? user = await _dbContext.Users.FirstOrDefaultAsync(x => x.Id == currentUserId);
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

        await _dbContext.SaveChangesAsync();

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

        bool userExists = await _dbContext.Users.AnyAsync(x => x.Id == userId);
        if (!userExists)
        {
            return ServiceResult<List<UserPostReadDto>>.Fail(ServiceErrorType.NotFound, "Kullanıcı bulunamadı.");
        }

        List<UserPostReadDto> posts = await _dbContext.Posts
            .AsNoTracking()
            .Where(x => x.AuthorId == userId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip(skip)
            .Take(take)
            .Select(x => new UserPostReadDto
            {
                Id = x.Id,
                Text = x.Text,
                CreatedAtUtc = x.CreatedAtUtc,
                ReplyToPostId = x.ReplyToPostId,
                LikeCount = x.Likes.Count,
                CommentCount = x.Comments.Count
            })
            .ToListAsync();

        return ServiceResult<List<UserPostReadDto>>.Success(posts);
    }

    public async Task<ServiceResult<List<UserCommentReadDto>>> GetUserCommentsAsync(Guid userId, int skip, int take)
    {
        ServiceError? paginationError = ValidatePagination(skip, take);
        if (paginationError != null)
        {
            return ServiceResult<List<UserCommentReadDto>>.Fail(paginationError.Type, paginationError.Message);
        }

        bool userExists = await _dbContext.Users.AnyAsync(x => x.Id == userId);
        if (!userExists)
        {
            return ServiceResult<List<UserCommentReadDto>>.Fail(ServiceErrorType.NotFound, "Kullanıcı bulunamadı.");
        }

        List<UserCommentReadDto> comments = await _dbContext.Comments
            .AsNoTracking()
            .Where(x => x.AuthorId == userId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip(skip)
            .Take(take)
            .Select(x => new UserCommentReadDto
            {
                Id = x.Id,
                PostId = x.PostId,
                Body = x.Body,
                ParentCommentId = x.ParentCommentId,
                CreatedAtUtc = x.CreatedAtUtc
            })
            .ToListAsync();

        return ServiceResult<List<UserCommentReadDto>>.Success(comments);
    }

    public async Task<ServiceResult<List<UserLikedPostReadDto>>> GetLikedPostsAsync(Guid userId, int skip, int take)
    {
        ServiceError? paginationError = ValidatePagination(skip, take);
        if (paginationError != null)
        {
            return ServiceResult<List<UserLikedPostReadDto>>.Fail(paginationError.Type, paginationError.Message);
        }

        bool userExists = await _dbContext.Users.AnyAsync(x => x.Id == userId);
        if (!userExists)
        {
            return ServiceResult<List<UserLikedPostReadDto>>.Fail(ServiceErrorType.NotFound, "Kullanıcı bulunamadı.");
        }

        List<UserLikedPostReadDto> likedPosts = await _dbContext.PostLikes
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip(skip)
            .Take(take)
            .Select(x => new UserLikedPostReadDto
            {
                PostId = x.PostId,
                AuthorId = x.Post.AuthorId,
                AuthorUserName = x.Post.Author.UserName ?? string.Empty,
                Text = x.Post.Text,
                PostCreatedAtUtc = x.Post.CreatedAtUtc,
                LikedAtUtc = x.CreatedAtUtc
            })
            .ToListAsync();

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

