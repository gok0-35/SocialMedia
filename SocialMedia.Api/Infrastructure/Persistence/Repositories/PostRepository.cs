using Microsoft.EntityFrameworkCore;
using SocialMedia.Api.Application.Dtos.Posts;
using SocialMedia.Api.Application.Repositories.Abstractions;
using SocialMedia.Api.Domain.Entities;

namespace SocialMedia.Api.Infrastructure.Persistence.Repositories;

public class PostRepository : IPostRepository
{
    private readonly AppDbContext _dbContext;

    public PostRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Post?> GetByIdAsync(Guid postId)
    {
        return _dbContext.Posts.FirstOrDefaultAsync(x => x.Id == postId);
    }

    public Task<bool> ExistsAsync(Guid postId)
    {
        return _dbContext.Posts.AnyAsync(x => x.Id == postId);
    }

    public Task AddAsync(Post post)
    {
        _dbContext.Posts.Add(post);
        return Task.CompletedTask;
    }

    public void Remove(Post post)
    {
        _dbContext.Posts.Remove(post);
    }

    public Task<PostLike?> GetLikeAsync(Guid userId, Guid postId)
    {
        return _dbContext.PostLikes.FindAsync(userId, postId).AsTask();
    }

    public Task AddLikeAsync(PostLike like)
    {
        _dbContext.PostLikes.Add(like);
        return Task.CompletedTask;
    }

    public void RemoveLike(PostLike like)
    {
        _dbContext.PostLikes.Remove(like);
    }

    public Task<int> CountLikesAsync(Guid postId)
    {
        return _dbContext.PostLikes.CountAsync(x => x.PostId == postId);
    }

    public Task<List<LikeUserReadDto>> GetLikesAsync(Guid postId, int skip, int take)
    {
        return _dbContext.PostLikes
            .AsNoTracking()
            .Where(x => x.PostId == postId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip(skip)
            .Take(take)
            .Select(x => new LikeUserReadDto
            {
                UserId = x.UserId,
                UserName = x.User.UserName ?? string.Empty,
                LikedAtUtc = x.CreatedAtUtc
            })
            .ToListAsync();
    }

    public async Task<List<PostSummaryReadDto>> GetPostsAsync(int skip, int take, Guid? authorId, string? normalizedTag)
    {
        IQueryable<Post> query = _dbContext.Posts.AsNoTracking();

        if (authorId.HasValue)
        {
            query = query.Where(x => x.AuthorId == authorId.Value);
        }

        if (!string.IsNullOrWhiteSpace(normalizedTag))
        {
            query = query.Where(x => x.PostTags.Any(pt => pt.Tag.Name == normalizedTag));
        }

        List<PostSummaryReadDto> posts = await BuildPostSummaryQuery(query)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync();

        await PopulateTagsAsync(posts);
        return posts;
    }

    public async Task<List<PostSummaryReadDto>> GetFeedAsync(Guid currentUserId, int skip, int take)
    {
        IQueryable<Guid> followingIdsQuery = _dbContext.Follows
            .AsNoTracking()
            .Where(x => x.FollowerId == currentUserId)
            .Select(x => x.FollowingId);

        IQueryable<Post> feedQuery = _dbContext.Posts.AsNoTracking()
            .Where(x => x.AuthorId == currentUserId || followingIdsQuery.Contains(x.AuthorId));

        List<PostSummaryReadDto> posts = await BuildPostSummaryQuery(feedQuery)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync();

        await PopulateTagsAsync(posts);
        return posts;
    }

    public async Task<PostSummaryReadDto?> GetSummaryByIdAsync(Guid postId)
    {
        PostSummaryReadDto? post = await BuildPostSummaryQuery(_dbContext.Posts.AsNoTracking().Where(x => x.Id == postId))
            .FirstOrDefaultAsync();

        if (post == null)
        {
            return null;
        }

        await PopulateTagsAsync(new List<PostSummaryReadDto> { post });
        return post;
    }

    public async Task<List<PostSummaryReadDto>> GetRepliesAsync(Guid postId, int skip, int take)
    {
        IQueryable<Post> query = _dbContext.Posts.AsNoTracking().Where(x => x.ReplyToPostId == postId);

        List<PostSummaryReadDto> replies = await BuildPostSummaryQuery(query)
            .OrderBy(x => x.CreatedAtUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync();

        await PopulateTagsAsync(replies);
        return replies;
    }

    public async Task ReplacePostTagsAsync(Guid postId, List<string> normalizedTags)
    {
        List<PostTag> existingRelations = await _dbContext.PostTags
            .Where(x => x.PostId == postId)
            .ToListAsync();

        if (existingRelations.Count > 0)
        {
            _dbContext.PostTags.RemoveRange(existingRelations);
        }

        if (normalizedTags.Count == 0)
        {
            return;
        }

        List<Tag> existingTags = await _dbContext.Tags
            .Where(x => normalizedTags.Contains(x.Name))
            .ToListAsync();

        Dictionary<string, Tag> tagsByName = existingTags
            .ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);

        foreach (string tagName in normalizedTags)
        {
            if (!tagsByName.TryGetValue(tagName, out Tag? tag))
            {
                tag = new Tag
                {
                    Id = Guid.NewGuid(),
                    Name = tagName
                };

                _dbContext.Tags.Add(tag);
                tagsByName[tagName] = tag;
            }

            _dbContext.PostTags.Add(new PostTag
            {
                PostId = postId,
                TagId = tag.Id
            });
        }
    }

    public Task SaveChangesAsync()
    {
        return _dbContext.SaveChangesAsync();
    }

    private static IQueryable<PostSummaryReadDto> BuildPostSummaryQuery(IQueryable<Post> query)
    {
        return query.Select(x => new PostSummaryReadDto
        {
            Id = x.Id,
            AuthorId = x.AuthorId,
            AuthorUserName = x.Author.UserName ?? string.Empty,
            Text = x.Text,
            ReplyToPostId = x.ReplyToPostId,
            CreatedAtUtc = x.CreatedAtUtc,
            LikeCount = x.Likes.Count,
            CommentCount = x.Comments.Count,
            ReplyCount = x.Replies.Count
        });
    }

    private async Task PopulateTagsAsync(List<PostSummaryReadDto> posts)
    {
        if (posts.Count == 0)
        {
            return;
        }

        List<Guid> postIds = posts.Select(x => x.Id).ToList();

        var tagRows = await _dbContext.PostTags
            .AsNoTracking()
            .Where(x => postIds.Contains(x.PostId))
            .Select(x => new { x.PostId, TagName = x.Tag.Name })
            .ToListAsync();

        Dictionary<Guid, List<string>> tagsByPostId = tagRows
            .GroupBy(x => x.PostId)
            .ToDictionary(
                x => x.Key,
                x => x.Select(t => t.TagName).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(t => t).ToList());

        foreach (PostSummaryReadDto post in posts)
        {
            if (tagsByPostId.TryGetValue(post.Id, out List<string>? tags))
            {
                post.Tags = tags;
            }
        }
    }
}
