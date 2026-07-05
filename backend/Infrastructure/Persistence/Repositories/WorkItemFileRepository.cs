using backend.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public class WorkItemFileRepository
    : GenericRepository<WorkItemFile>, IWorkItemFileRepository
{
    public WorkItemFileRepository(AppDbContext context)
        : base(context)
    {
    }

    public async Task<List<WorkItemFile>> GetByWorkItemIdAsync(int workItemId)
    {
        return await _dbSet
            .Where(x => x.WorkItemId == workItemId && x.SubWorkItemId == null)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<WorkItemFile>> GetBySubWorkItemIdAsync(int subWorkItemId)
    {
        return await _dbSet
            .Where(x => x.SubWorkItemId == subWorkItemId)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync();
    }
}