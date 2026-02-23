using SocialMedia.Api.Application.Dtos.Comments;
using SocialMedia.Api.Application.Dtos.Common;

namespace SocialMedia.Api.Application.Services.Abstractions;

public interface ICommentService
{
    Task<ServiceResult<List<CommentReadDto>>> GetByPostAsync(Guid postId, int skip, int take);
    Task<ServiceResult<CommentReadDto>> GetByIdAsync(Guid commentId);
    Task<ServiceResult<List<CommentReadDto>>> GetChildrenAsync(Guid commentId, int skip, int take);
    Task<ServiceResult<CreatedCommentReadDto>> CreateAsync(Guid currentUserId, Guid postId, CreateCommentWriteDto request);
    Task<ServiceResult<MessageReadDto>> DeleteAsync(Guid currentUserId, Guid commentId);
    Task<ServiceResult<MessageReadDto>> UpdateAsync(Guid currentUserId, Guid commentId, UpdateCommentWriteDto request);
}

