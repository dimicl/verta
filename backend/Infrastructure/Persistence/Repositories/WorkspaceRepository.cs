using backend.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public class WorkspaceRepository: GenericRepository<Workspace>, IWorkspaceRepository
{
    public WorkspaceRepository(AppDbContext context): base(context){}

   
    public async Task<Workspace?> GetByOwnerIdAsync(int ownerId)
    {
        return  await _dbSet.FirstOrDefaultAsync(workspace => workspace.OwnerId == ownerId);
    }

}