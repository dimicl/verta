public interface IWorkspaceRepository : IGenericRepository<Workspace>
{
    Task<Workspace?> GetByOwnerIdAsync(int ownerId);
}