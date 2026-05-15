public interface IWorkspaceMemberRepository : IGenericRepository<WorkspaceMember>
{
    Task<WorkspaceMember?> GetByWorkspaceAndUserIdAsync(int workspaceId, int userId);

    Task<List<WorkspaceMember>> GetByWorkspaceIdAsync(int workspaceId);

    Task<List<WorkspaceMember>> GetByUserIdAsync(int userId);
}