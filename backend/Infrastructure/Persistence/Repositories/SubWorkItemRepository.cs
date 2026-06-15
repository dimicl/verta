using backend.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public class SubWorkItemRepository 
    : GenericRepository<SubWorkItem>, ISubWorkItemRepository
{
    public SubWorkItemRepository(AppDbContext context) 
        : base(context)
    {
    }

    public async Task<List<SubWorkItem>> GetByWorkItemIdAsync(int workItemId)
    {
        return await _dbSet
            .Where(x => x.WorkItemId == workItemId)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync();
    }
}