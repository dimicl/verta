using backend.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public class WorkItemLockRepository : GenericRepository<WorkItemLock>, IWorkItemLockRepository
{
    public WorkItemLockRepository(AppDbContext context) : base(context) { }

    public async Task<WorkItemLock?> GetByWorkItemIdAsync(int workItemId)
    {
        return await _dbSet.FirstOrDefaultAsync(x => x.WorkItemId == workItemId);
    }

    public async Task<List<WorkItemLock>> GetExpiredAsync()
    {
        var now = DateTime.UtcNow;
        return await _dbSet.Where(x => x.ExpiresAt < now).ToListAsync();
    }
}