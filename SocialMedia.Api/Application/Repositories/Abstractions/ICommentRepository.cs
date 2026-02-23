using SocialMedia.Api.Application.Dtos.Comments;
using SocialMedia.Api.Domain.Entities;

namespace SocialMedia.Api.Application.Repositories.Abstractions;

public interface ICommentRepository
{
    Task<bool> PostExistsAsync(Guid postId);
    Task<bool> ExistsAsync(Guid commentId);
    Task<List<CommentReadDto>> GetByPostAsync(Guid postId, int skip, int take);
    Task<CommentReadDto?> GetReadByIdAsync(Guid commentId);
    Task<List<CommentReadDto>> GetChildrenAsync(Guid commentId, int skip, int take);

    Task<Comment?> GetByIdAsync(Guid commentId);
    Task<Comment?> GetParentCommentAsync(Guid parentCommentId);
    Task AddAsync(Comment comment);
    void Remove(Comment comment);

    Task SaveChangesAsync();
}
