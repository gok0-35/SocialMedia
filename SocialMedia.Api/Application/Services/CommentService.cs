using SocialMedia.Api.Application.Dtos.Comments;
using SocialMedia.Api.Application.Dtos.Common;
using SocialMedia.Api.Application.Repositories.Abstractions;
using SocialMedia.Api.Application.Services.Abstractions;
using SocialMedia.Api.Domain.Entities;

namespace SocialMedia.Api.Application.Services;

public class CommentService : ICommentService
{
    private readonly ICommentRepository _commentRepository;

    public CommentService(ICommentRepository commentRepository)
    {
        _commentRepository = commentRepository;
    }

    public async Task<ServiceResult<List<CommentReadDto>>> GetByPostAsync(Guid postId, int skip, int take)
    {
        ServiceError? paginationError = ValidatePagination(skip, take);
        if (paginationError != null)
        {
            return ServiceResult<List<CommentReadDto>>.Fail(paginationError.Type, paginationError.Message);
        }

        bool postExists = await _commentRepository.PostExistsAsync(postId);
        if (!postExists)
        {
            return ServiceResult<List<CommentReadDto>>.Fail(ServiceErrorType.NotFound, "Post bulunamadı.");
        }

        List<CommentReadDto> comments = await _commentRepository.GetByPostAsync(postId, skip, take);

        return ServiceResult<List<CommentReadDto>>.Success(comments);
    }

    public async Task<ServiceResult<CommentReadDto>> GetByIdAsync(Guid commentId)
    {
        CommentReadDto? comment = await _commentRepository.GetReadByIdAsync(commentId);

        if (comment == null)
        {
            return ServiceResult<CommentReadDto>.Fail(ServiceErrorType.NotFound, "Yorum bulunamadı.");
        }

        return ServiceResult<CommentReadDto>.Success(comment);
    }

    public async Task<ServiceResult<List<CommentReadDto>>> GetChildrenAsync(Guid commentId, int skip, int take)
    {
        ServiceError? paginationError = ValidatePagination(skip, take);
        if (paginationError != null)
        {
            return ServiceResult<List<CommentReadDto>>.Fail(paginationError.Type, paginationError.Message);
        }

        bool commentExists = await _commentRepository.ExistsAsync(commentId);
        if (!commentExists)
        {
            return ServiceResult<List<CommentReadDto>>.Fail(ServiceErrorType.NotFound, "Yorum bulunamadı.");
        }

        List<CommentReadDto> children = await _commentRepository.GetChildrenAsync(commentId, skip, take);

        return ServiceResult<List<CommentReadDto>>.Success(children);
    }

    public async Task<ServiceResult<CreatedCommentReadDto>> CreateAsync(Guid currentUserId, Guid postId, CreateCommentWriteDto request)
    {
        if (request == null)
        {
            return ServiceResult<CreatedCommentReadDto>.Fail(ServiceErrorType.BadRequest, "Body boş olamaz.");
        }

        if (string.IsNullOrWhiteSpace(request.Body))
        {
            return ServiceResult<CreatedCommentReadDto>.Fail(ServiceErrorType.BadRequest, "Body zorunlu.");
        }

        string body = request.Body.Trim();
        if (body.Length > 2000)
        {
            return ServiceResult<CreatedCommentReadDto>.Fail(ServiceErrorType.BadRequest, "Yorum en fazla 2000 karakter olabilir.");
        }

        bool postExists = await _commentRepository.PostExistsAsync(postId);
        if (!postExists)
        {
            return ServiceResult<CreatedCommentReadDto>.Fail(ServiceErrorType.NotFound, "Post bulunamadı.");
        }

        if (request.ParentCommentId.HasValue)
        {
            Comment? parentComment = await _commentRepository.GetParentCommentAsync(request.ParentCommentId.Value);

            if (parentComment == null)
            {
                return ServiceResult<CreatedCommentReadDto>.Fail(ServiceErrorType.BadRequest, "ParentCommentId geçersiz.");
            }

            if (parentComment.PostId != postId)
            {
                return ServiceResult<CreatedCommentReadDto>.Fail(ServiceErrorType.BadRequest, "ParentComment aynı post içinde olmalı.");
            }
        }

        Comment comment = new()
        {
            Id = Guid.NewGuid(),
            PostId = postId,
            AuthorId = currentUserId,
            Body = body,
            ParentCommentId = request.ParentCommentId
        };

        await _commentRepository.AddAsync(comment);
        await _commentRepository.SaveChangesAsync();

        return ServiceResult<CreatedCommentReadDto>.Success(new CreatedCommentReadDto
        {
            Message = "Yorum eklendi.",
            CommentId = comment.Id
        });
    }

    public async Task<ServiceResult<MessageReadDto>> DeleteAsync(Guid currentUserId, Guid commentId)
    {
        Comment? comment = await _commentRepository.GetByIdAsync(commentId);
        if (comment == null)
        {
            return ServiceResult<MessageReadDto>.Fail(ServiceErrorType.NotFound, "Yorum bulunamadı.");
        }

        if (comment.AuthorId != currentUserId)
        {
            return ServiceResult<MessageReadDto>.Fail(ServiceErrorType.Forbidden, "Forbidden");
        }

        _commentRepository.Remove(comment);
        await _commentRepository.SaveChangesAsync();

        return ServiceResult<MessageReadDto>.Success(new MessageReadDto
        {
            Message = "Yorum silindi."
        });
    }

    public async Task<ServiceResult<MessageReadDto>> UpdateAsync(Guid currentUserId, Guid commentId, UpdateCommentWriteDto request)
    {
        if (request == null)
        {
            return ServiceResult<MessageReadDto>.Fail(ServiceErrorType.BadRequest, "Body boş olamaz.");
        }

        if (string.IsNullOrWhiteSpace(request.Body))
        {
            return ServiceResult<MessageReadDto>.Fail(ServiceErrorType.BadRequest, "Body zorunlu.");
        }

        string body = request.Body.Trim();
        if (body.Length > 2000)
        {
            return ServiceResult<MessageReadDto>.Fail(ServiceErrorType.BadRequest, "Yorum en fazla 2000 karakter olabilir.");
        }

        Comment? comment = await _commentRepository.GetByIdAsync(commentId);
        if (comment == null)
        {
            return ServiceResult<MessageReadDto>.Fail(ServiceErrorType.NotFound, "Yorum bulunamadı.");
        }

        if (comment.AuthorId != currentUserId)
        {
            return ServiceResult<MessageReadDto>.Fail(ServiceErrorType.Forbidden, "Forbidden");
        }

        comment.Body = body;
        await _commentRepository.SaveChangesAsync();

        return ServiceResult<MessageReadDto>.Success(new MessageReadDto
        {
            Message = "Yorum güncellendi."
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
