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
            .Include(x => x.User)
            .Where(x => x.WorkItemId == workItemId && x.SubWorkItemId == null)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Comment>> GetBySubWorkItemIdAsync(int subWorkItemId)
    {
        return await _dbSet
            .Include(x => x.User)
            .Where(x => x.SubWorkItemId == subWorkItemId)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task<Comment?> GetByIdWithUserAsync(int id)
    {
        return await _dbSet
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == id);
    }
}