using Microsoft.EntityFrameworkCore;
using SocialMedia.Api.Application.Dtos.Comments;
using SocialMedia.Api.Application.Repositories.Abstractions;
using SocialMedia.Api.Domain.Entities;

namespace SocialMedia.Api.Infrastructure.Persistence.Repositories;

public class CommentRepository : ICommentRepository
{
    private readonly AppDbContext _dbContext;

    public CommentRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> PostExistsAsync(Guid postId)
    {
        return _dbContext.Posts.AnyAsync(x => x.Id == postId);
    }

    public Task<bool> ExistsAsync(Guid commentId)
    {
        return _dbContext.Comments.AnyAsync(x => x.Id == commentId);
    }

    public Task<List<CommentReadDto>> GetByPostAsync(Guid postId, int skip, int take)
    {
        return BuildReadQuery(_dbContext.Comments.AsNoTracking().Where(x => x.PostId == postId))
            .OrderBy(x => x.CreatedAtUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public Task<CommentReadDto?> GetReadByIdAsync(Guid commentId)
    {
        return BuildReadQuery(_dbContext.Comments.AsNoTracking().Where(x => x.Id == commentId))
            .FirstOrDefaultAsync();
    }

    public Task<List<CommentReadDto>> GetChildrenAsync(Guid commentId, int skip, int take)
    {
        return BuildReadQuery(_dbContext.Comments.AsNoTracking().Where(x => x.ParentCommentId == commentId))
            .OrderBy(x => x.CreatedAtUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public Task<Comment?> GetByIdAsync(Guid commentId)
    {
        return _dbContext.Comments.FirstOrDefaultAsync(x => x.Id == commentId);
    }

    public Task<Comment?> GetParentCommentAsync(Guid parentCommentId)
    {
        return _dbContext.Comments
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == parentCommentId);
    }

    public Task AddAsync(Comment comment)
    {
        _dbContext.Comments.Add(comment);
        return Task.CompletedTask;
    }

    public void Remove(Comment comment)
    {
        _dbContext.Comments.Remove(comment);
    }

    public Task SaveChangesAsync()
    {
        return _dbContext.SaveChangesAsync();
    }

    private static IQueryable<CommentReadDto> BuildReadQuery(IQueryable<Comment> query)
    {
        return query.Select(x => new CommentReadDto
        {
            Id = x.Id,
            PostId = x.PostId,
            AuthorId = x.AuthorId,
            AuthorUserName = x.Author.UserName ?? string.Empty,
            Body = x.Body,
            ParentCommentId = x.ParentCommentId,
            CreatedAtUtc = x.CreatedAtUtc,
            ChildrenCount = x.Children.Count
        });
    }
}
