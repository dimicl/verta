using backend.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public class WorkItemRepository : GenericRepository<WorkItem>, IWorkItemRepository
{
    public WorkItemRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<List<WorkItem>> GetByBoardIdAsync(int boardId)
    {
        return await _dbSet
            .Where(x => x.BoardId == boardId)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<WorkItem>> GetBySprintIdAsync(int sprintId)
    {
        return await _dbSet
            .Where(x => x.SprintId == sprintId)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync();
    }
}