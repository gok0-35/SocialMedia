using Microsoft.EntityFrameworkCore;
using SocialMedia.Api.Application.Dtos.Common;
using SocialMedia.Api.Application.Dtos.Posts;
using SocialMedia.Api.Application.Services.Abstractions;
using SocialMedia.Api.Domain.Entities;
using SocialMedia.Api.Infrastructure.Persistence;

namespace SocialMedia.Api.Application.Services;

public class PostService : IPostService
{
    private readonly AppDbContext _dbContext;

    public PostService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<ServiceResult<CreatedPostReadDto>> CreateAsync(Guid currentUserId, CreatePostWriteDto request)
    {
        if (request == null)
        {
            return Task.FromResult(ServiceResult<CreatedPostReadDto>.Fail(ServiceErrorType.BadRequest, "Body boş olamaz."));
        }

        return CreateCoreAsync(currentUserId, request.Text, request.Tags, request.ReplyToPostId);
    }

    public Task<ServiceResult<CreatedPostReadDto>> CreateReplyAsync(Guid currentUserId, Guid postId, CreateReplyWriteDto request)
    {
        if (request == null)
        {
            return Task.FromResult(ServiceResult<CreatedPostReadDto>.Fail(ServiceErrorType.BadRequest, "Body boş olamaz."));
        }

        return CreateCoreAsync(currentUserId, request.Text, request.Tags, postId);
    }

    public async Task<ServiceResult<MessageReadDto>> UpdateAsync(Guid currentUserId, Guid postId, UpdatePostWriteDto request)
    {
        if (request == null)
        {
            return ServiceResult<MessageReadDto>.Fail(ServiceErrorType.BadRequest, "Body boş olamaz.");
        }

        if (string.IsNullOrWhiteSpace(request.Text))
        {
            return ServiceResult<MessageReadDto>.Fail(ServiceErrorType.BadRequest, "Text zorunlu.");
        }

        string text = request.Text.Trim();
        if (text.Length > 280)
        {
            return ServiceResult<MessageReadDto>.Fail(ServiceErrorType.BadRequest, "Text en fazla 280 karakter olabilir.");
        }

        Post? post = await _dbContext.Posts.FirstOrDefaultAsync(x => x.Id == postId);
        if (post == null)
        {
            return ServiceResult<MessageReadDto>.Fail(ServiceErrorType.NotFound, "Post bulunamadı.");
        }

        if (post.AuthorId != currentUserId)
        {
            return ServiceResult<MessageReadDto>.Fail(ServiceErrorType.Forbidden, "Forbidden");
        }

        post.Text = text;

        if (request.Tags != null)
        {
            List<string> normalizedTags = NormalizeTagNames(request.Tags);
            if (normalizedTags.Count > 10)
            {
                return ServiceResult<MessageReadDto>.Fail(ServiceErrorType.BadRequest, "Bir post en fazla 10 tag içerebilir.");
            }

            await ReplacePostTagsAsync(post.Id, normalizedTags);
        }

        await _dbContext.SaveChangesAsync();

        return ServiceResult<MessageReadDto>.Success(new MessageReadDto
        {
            Message = "Post güncellendi."
        });
    }

    public async Task<ServiceResult<List<PostSummaryReadDto>>> GetPostsAsync(int skip, int take, Guid? authorId, string? tag)
    {
        ServiceError? paginationError = ValidatePagination(skip, take);
        if (paginationError != null)
        {
            return ServiceResult<List<PostSummaryReadDto>>.Fail(paginationError.Type, paginationError.Message);
        }

        IQueryable<Post> query = _dbContext.Posts.AsNoTracking();

        if (authorId.HasValue)
        {
            query = query.Where(x => x.AuthorId == authorId.Value);
        }

        if (!string.IsNullOrWhiteSpace(tag))
        {
            string normalizedTag = NormalizeTagName(tag);
            query = query.Where(x => x.PostTags.Any(pt => pt.Tag.Name == normalizedTag));
        }

        List<PostSummaryReadDto> posts = await BuildPostSummaryQuery(query)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync();

        await PopulateTagsAsync(posts);
        return ServiceResult<List<PostSummaryReadDto>>.Success(posts);
    }

    public async Task<ServiceResult<List<PostSummaryReadDto>>> GetFeedAsync(Guid currentUserId, int skip, int take)
    {
        ServiceError? paginationError = ValidatePagination(skip, take);
        if (paginationError != null)
        {
            return ServiceResult<List<PostSummaryReadDto>>.Fail(paginationError.Type, paginationError.Message);
        }

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
        return ServiceResult<List<PostSummaryReadDto>>.Success(posts);
    }

    public async Task<ServiceResult<PostSummaryReadDto>> GetByIdAsync(Guid postId)
    {
        PostSummaryReadDto? post = await BuildPostSummaryQuery(_dbContext.Posts.AsNoTracking().Where(x => x.Id == postId))
            .FirstOrDefaultAsync();

        if (post == null)
        {
            return ServiceResult<PostSummaryReadDto>.Fail(ServiceErrorType.NotFound, "Post bulunamadı.");
        }

        await PopulateTagsAsync(new List<PostSummaryReadDto> { post });
        return ServiceResult<PostSummaryReadDto>.Success(post);
    }

    public async Task<ServiceResult<List<PostSummaryReadDto>>> GetRepliesAsync(Guid postId, int skip, int take)
    {
        ServiceError? paginationError = ValidatePagination(skip, take);
        if (paginationError != null)
        {
            return ServiceResult<List<PostSummaryReadDto>>.Fail(paginationError.Type, paginationError.Message);
        }

        bool postExists = await _dbContext.Posts.AnyAsync(x => x.Id == postId);
        if (!postExists)
        {
            return ServiceResult<List<PostSummaryReadDto>>.Fail(ServiceErrorType.NotFound, "Post bulunamadı.");
        }

        IQueryable<Post> query = _dbContext.Posts.AsNoTracking().Where(x => x.ReplyToPostId == postId);

        List<PostSummaryReadDto> replies = await BuildPostSummaryQuery(query)
            .OrderBy(x => x.CreatedAtUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync();

        await PopulateTagsAsync(replies);
        return ServiceResult<List<PostSummaryReadDto>>.Success(replies);
    }

    public async Task<ServiceResult<MessageReadDto>> DeleteAsync(Guid currentUserId, Guid postId)
    {
        Post? post = await _dbContext.Posts.FirstOrDefaultAsync(x => x.Id == postId);
        if (post == null)
        {
            return ServiceResult<MessageReadDto>.Fail(ServiceErrorType.NotFound, "Post bulunamadı.");
        }

        if (post.AuthorId != currentUserId)
        {
            return ServiceResult<MessageReadDto>.Fail(ServiceErrorType.Forbidden, "Forbidden");
        }

        _dbContext.Posts.Remove(post);
        await _dbContext.SaveChangesAsync();

        return ServiceResult<MessageReadDto>.Success(new MessageReadDto
        {
            Message = "Post silindi."
        });
    }

    public async Task<ServiceResult<MessageReadDto>> LikeAsync(Guid currentUserId, Guid postId)
    {
        bool postExists = await _dbContext.Posts.AnyAsync(x => x.Id == postId);
        if (!postExists)
        {
            return ServiceResult<MessageReadDto>.Fail(ServiceErrorType.NotFound, "Post bulunamadı.");
        }

        PostLike? existingLike = await _dbContext.PostLikes.FindAsync(currentUserId, postId);
        if (existingLike != null)
        {
            return ServiceResult<MessageReadDto>.Success(new MessageReadDto
            {
                Message = "Post zaten beğenilmiş."
            });
        }

        _dbContext.PostLikes.Add(new PostLike
        {
            UserId = currentUserId,
            PostId = postId
        });

        await _dbContext.SaveChangesAsync();

        return ServiceResult<MessageReadDto>.Success(new MessageReadDto
        {
            Message = "Post beğenildi."
        });
    }

    public async Task<ServiceResult<MessageReadDto>> UnlikeAsync(Guid currentUserId, Guid postId)
    {
        PostLike? existingLike = await _dbContext.PostLikes.FindAsync(currentUserId, postId);
        if (existingLike == null)
        {
            return ServiceResult<MessageReadDto>.Success(new MessageReadDto
            {
                Message = "Post daha önce beğenilmemiş."
            });
        }

        _dbContext.PostLikes.Remove(existingLike);
        await _dbContext.SaveChangesAsync();

        return ServiceResult<MessageReadDto>.Success(new MessageReadDto
        {
            Message = "Post beğenisi kaldırıldı."
        });
    }

    public async Task<ServiceResult<PostLikesReadDto>> GetLikesAsync(Guid postId, int skip, int take)
    {
        ServiceError? paginationError = ValidatePagination(skip, take);
        if (paginationError != null)
        {
            return ServiceResult<PostLikesReadDto>.Fail(paginationError.Type, paginationError.Message);
        }

        bool postExists = await _dbContext.Posts.AnyAsync(x => x.Id == postId);
        if (!postExists)
        {
            return ServiceResult<PostLikesReadDto>.Fail(ServiceErrorType.NotFound, "Post bulunamadı.");
        }

        int totalCount = await _dbContext.PostLikes.CountAsync(x => x.PostId == postId);

        List<LikeUserReadDto> users = await _dbContext.PostLikes
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

        return ServiceResult<PostLikesReadDto>.Success(new PostLikesReadDto
        {
            PostId = postId,
            TotalCount = totalCount,
            Items = users
        });
    }

    private async Task<ServiceResult<CreatedPostReadDto>> CreateCoreAsync(
        Guid currentUserId,
        string textInput,
        IEnumerable<string>? tagsInput,
        Guid? replyToPostId)
    {
        if (string.IsNullOrWhiteSpace(textInput))
        {
            return ServiceResult<CreatedPostReadDto>.Fail(ServiceErrorType.BadRequest, "Text zorunlu.");
        }

        string text = textInput.Trim();
        if (text.Length > 280)
        {
            return ServiceResult<CreatedPostReadDto>.Fail(ServiceErrorType.BadRequest, "Text en fazla 280 karakter olabilir.");
        }

        if (replyToPostId.HasValue)
        {
            bool replyTargetExists = await _dbContext.Posts.AnyAsync(x => x.Id == replyToPostId.Value);
            if (!replyTargetExists)
            {
                return ServiceResult<CreatedPostReadDto>.Fail(ServiceErrorType.BadRequest, "Yanıtlanacak post bulunamadı.");
            }
        }

        List<string> normalizedTags = NormalizeTagNames(tagsInput);
        if (normalizedTags.Count > 10)
        {
            return ServiceResult<CreatedPostReadDto>.Fail(ServiceErrorType.BadRequest, "Bir post en fazla 10 tag içerebilir.");
        }

        Post post = new Post
        {
            Id = Guid.NewGuid(),
            AuthorId = currentUserId,
            Text = text,
            ReplyToPostId = replyToPostId
        };

        _dbContext.Posts.Add(post);
        await ReplacePostTagsAsync(post.Id, normalizedTags);
        await _dbContext.SaveChangesAsync();

        return ServiceResult<CreatedPostReadDto>.Success(new CreatedPostReadDto
        {
            Message = "Post oluşturuldu.",
            PostId = post.Id
        });
    }

    private async Task ReplacePostTagsAsync(Guid postId, List<string> normalizedTags)
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

    private IQueryable<PostSummaryReadDto> BuildPostSummaryQuery(IQueryable<Post> query)
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

    private static List<string> NormalizeTagNames(IEnumerable<string>? tags)
    {
        HashSet<string> normalized = new(StringComparer.OrdinalIgnoreCase);

        if (tags == null)
        {
            return normalized.ToList();
        }

        foreach (string? rawTag in tags)
        {
            if (string.IsNullOrWhiteSpace(rawTag))
            {
                continue;
            }

            string tag = NormalizeTagName(rawTag);
            if (tag.Length == 0)
            {
                continue;
            }

            if (tag.Length > 50)
            {
                tag = tag[..50];
            }

            normalized.Add(tag);
        }

        return normalized.ToList();
    }

    private static string NormalizeTagName(string rawTag)
    {
        string tag = rawTag.Trim();
        while (tag.StartsWith('#'))
        {
            tag = tag[1..];
        }

        return tag.Trim().ToLowerInvariant();
    }
}

