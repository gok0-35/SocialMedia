using Microsoft.EntityFrameworkCore;
using SocialMedia.Api.Application.Dtos.Users;
using SocialMedia.Api.Application.Repositories.Abstractions;
using SocialMedia.Api.Domain.Entities;

namespace SocialMedia.Api.Infrastructure.Persistence.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _dbContext;

    public UserRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<UserProfileReadDto?> GetProfileAsync(Guid userId)
    {
        return _dbContext.Users
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
    }

    public Task<MyProfileReadDto?> GetMyProfileAsync(Guid currentUserId)
    {
        return _dbContext.Users
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
    }

    public Task<ApplicationUser?> GetByIdAsync(Guid userId)
    {
        return _dbContext.Users.FirstOrDefaultAsync(x => x.Id == userId);
    }

    public Task<bool> ExistsAsync(Guid userId)
    {
        return _dbContext.Users.AnyAsync(x => x.Id == userId);
    }

    public Task<List<UserPostReadDto>> GetPostsAsync(Guid userId, int skip, int take)
    {
        return _dbContext.Posts
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
    }

    public Task<List<UserCommentReadDto>> GetCommentsAsync(Guid userId, int skip, int take)
    {
        return _dbContext.Comments
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
    }

    public Task<List<UserLikedPostReadDto>> GetLikedPostsAsync(Guid userId, int skip, int take)
    {
        return _dbContext.PostLikes
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
    }

    public Task SaveChangesAsync()
    {
        return _dbContext.SaveChangesAsync();
    }
}
