using backend.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public class CommentRepository
    : GenericRepository<Comment>, ICommentRepository
{
    public CommentRepository(AppDbContext context)
        : base(context)
    {
    }

    public async Task<List<Comment>> GetByWorkItemIdAsync(int workItemId)
    {
        return await _dbSet
            .Where(x => x.WorkItemId == workItemId)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync();
    }
}