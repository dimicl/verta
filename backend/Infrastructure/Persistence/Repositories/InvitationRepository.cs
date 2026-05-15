using backend.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public class InvitationRepository : GenericRepository<Invitation>, IInvitationRepository
{
    public InvitationRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<Invitation?> GetByWorkspaceAndUserIdAsync(int workspaceId, int userId)
    {
        return await _dbSet
            .FirstOrDefaultAsync(x => x.WorkspaceId == workspaceId && x.UserId == userId);
    }

    public async Task<List<Invitation>> GetByWorkspaceIdAsync(int workspaceId)
    {
        return await _dbSet
            .Where(x => x.WorkspaceId == workspaceId)
            .ToListAsync();
    }

    public async Task<List<Invitation>> GetByUserIdAsync(int userId)
    {
        return await _dbSet
            .Where(x => x.UserId == userId)
            .ToListAsync();
    }
}