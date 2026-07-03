using backend.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public class SprintRepository : GenericRepository<Sprint>, ISprintRepository
{
    public SprintRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<List<Sprint>> GetByBoardIdAsync(int boardId)
    {
        return await _dbSet
            .Where(x => x.BoardId == boardId)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync();
    }
}
