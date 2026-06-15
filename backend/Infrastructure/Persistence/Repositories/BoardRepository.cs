using backend.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public class BoardRepository : GenericRepository<Board>, IBoardRepository
{
    public BoardRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<List<Board>> GetByWorkspaceIdAsync(int workspaceId)
    {
        return await _dbSet
            .Where(x => x.WorkspaceId == workspaceId)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync();
    }
}