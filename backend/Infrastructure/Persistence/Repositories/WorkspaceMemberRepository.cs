using backend.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public class WorkspaceMemberRepository
    : GenericRepository<WorkspaceMember>, IWorkspaceMemberRepository
{
    public WorkspaceMemberRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<WorkspaceMember?> GetByWorkspaceAndUserIdAsync(int workspaceId, int userId)
    {
        return await _dbSet
            .Include(x => x.User)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.WorkspaceId == workspaceId && x.UserId == userId);
    }

    public async Task<List<WorkspaceMember>> GetByWorkspaceIdAsync(int workspaceId)
    {
        return await _dbSet
            .Include(x => x.User)
            .AsNoTracking()
            .Where(x => x.WorkspaceId == workspaceId)
            .ToListAsync();
    }

    public async Task<List<WorkspaceMember>> GetByUserIdAsync(int userId)
    {
        return await _dbSet
            .Where(x => x.UserId == userId)
            .ToListAsync();
    }
}