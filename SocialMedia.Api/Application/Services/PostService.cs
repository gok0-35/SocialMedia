using SocialMedia.Api.Application.Dtos.Common;
using SocialMedia.Api.Application.Dtos.Posts;
using SocialMedia.Api.Application.Repositories.Abstractions;
using SocialMedia.Api.Application.Services.Abstractions;
using SocialMedia.Api.Domain.Entities;

namespace SocialMedia.Api.Application.Services;

public class PostService : IPostService
{
    private readonly IPostRepository _postRepository;

    public PostService(IPostRepository postRepository)
    {
        _postRepository = postRepository;
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

        Post? post = await _postRepository.GetByIdAsync(postId);
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

            await _postRepository.ReplacePostTagsAsync(post.Id, normalizedTags);
        }

        await _postRepository.SaveChangesAsync();

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

        string? normalizedTag = null;
        if (!string.IsNullOrWhiteSpace(tag))
        {
            normalizedTag = NormalizeTagName(tag);
        }

        List<PostSummaryReadDto> posts = await _postRepository.GetPostsAsync(skip, take, authorId, normalizedTag);
        return ServiceResult<List<PostSummaryReadDto>>.Success(posts);
    }

    public async Task<ServiceResult<List<PostSummaryReadDto>>> GetFeedAsync(Guid currentUserId, int skip, int take)
    {
        ServiceError? paginationError = ValidatePagination(skip, take);
        if (paginationError != null)
        {
            return ServiceResult<List<PostSummaryReadDto>>.Fail(paginationError.Type, paginationError.Message);
        }

        List<PostSummaryReadDto> posts = await _postRepository.GetFeedAsync(currentUserId, skip, take);
        return ServiceResult<List<PostSummaryReadDto>>.Success(posts);
    }

    public async Task<ServiceResult<PostSummaryReadDto>> GetByIdAsync(Guid postId)
    {
        PostSummaryReadDto? post = await _postRepository.GetSummaryByIdAsync(postId);
        if (post == null)
        {
            return ServiceResult<PostSummaryReadDto>.Fail(ServiceErrorType.NotFound, "Post bulunamadı.");
        }

        return ServiceResult<PostSummaryReadDto>.Success(post);
    }

    public async Task<ServiceResult<List<PostSummaryReadDto>>> GetRepliesAsync(Guid postId, int skip, int take)
    {
        ServiceError? paginationError = ValidatePagination(skip, take);
        if (paginationError != null)
        {
            return ServiceResult<List<PostSummaryReadDto>>.Fail(paginationError.Type, paginationError.Message);
        }

        bool postExists = await _postRepository.ExistsAsync(postId);
        if (!postExists)
        {
            return ServiceResult<List<PostSummaryReadDto>>.Fail(ServiceErrorType.NotFound, "Post bulunamadı.");
        }

        List<PostSummaryReadDto> replies = await _postRepository.GetRepliesAsync(postId, skip, take);
        return ServiceResult<List<PostSummaryReadDto>>.Success(replies);
    }

    public async Task<ServiceResult<MessageReadDto>> DeleteAsync(Guid currentUserId, Guid postId)
    {
        Post? post = await _postRepository.GetByIdAsync(postId);
        if (post == null)
        {
            return ServiceResult<MessageReadDto>.Fail(ServiceErrorType.NotFound, "Post bulunamadı.");
        }

        if (post.AuthorId != currentUserId)
        {
            return ServiceResult<MessageReadDto>.Fail(ServiceErrorType.Forbidden, "Forbidden");
        }

        _postRepository.Remove(post);
        await _postRepository.SaveChangesAsync();

        return ServiceResult<MessageReadDto>.Success(new MessageReadDto
        {
            Message = "Post silindi."
        });
    }

    public async Task<ServiceResult<MessageReadDto>> LikeAsync(Guid currentUserId, Guid postId)
    {
        bool postExists = await _postRepository.ExistsAsync(postId);
        if (!postExists)
        {
            return ServiceResult<MessageReadDto>.Fail(ServiceErrorType.NotFound, "Post bulunamadı.");
        }

        PostLike? existingLike = await _postRepository.GetLikeAsync(currentUserId, postId);
        if (existingLike != null)
        {
            return ServiceResult<MessageReadDto>.Success(new MessageReadDto
            {
                Message = "Post zaten beğenilmiş."
            });
        }

        await _postRepository.AddLikeAsync(new PostLike
        {
            UserId = currentUserId,
            PostId = postId
        });

        await _postRepository.SaveChangesAsync();

        return ServiceResult<MessageReadDto>.Success(new MessageReadDto
        {
            Message = "Post beğenildi."
        });
    }

    public async Task<ServiceResult<MessageReadDto>> UnlikeAsync(Guid currentUserId, Guid postId)
    {
        PostLike? existingLike = await _postRepository.GetLikeAsync(currentUserId, postId);
        if (existingLike == null)
        {
            return ServiceResult<MessageReadDto>.Success(new MessageReadDto
            {
                Message = "Post daha önce beğenilmemiş."
            });
        }

        _postRepository.RemoveLike(existingLike);
        await _postRepository.SaveChangesAsync();

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

        bool postExists = await _postRepository.ExistsAsync(postId);
        if (!postExists)
        {
            return ServiceResult<PostLikesReadDto>.Fail(ServiceErrorType.NotFound, "Post bulunamadı.");
        }

        int totalCount = await _postRepository.CountLikesAsync(postId);

        List<LikeUserReadDto> users = await _postRepository.GetLikesAsync(postId, skip, take);

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
            bool replyTargetExists = await _postRepository.ExistsAsync(replyToPostId.Value);
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

        Post post = new()
        {
            Id = Guid.NewGuid(),
            AuthorId = currentUserId,
            Text = text,
            ReplyToPostId = replyToPostId
        };

        await _postRepository.AddAsync(post);
        await _postRepository.ReplacePostTagsAsync(post.Id, normalizedTags);
        await _postRepository.SaveChangesAsync();

        return ServiceResult<CreatedPostReadDto>.Success(new CreatedPostReadDto
        {
            Message = "Post oluşturuldu.",
            PostId = post.Id
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
