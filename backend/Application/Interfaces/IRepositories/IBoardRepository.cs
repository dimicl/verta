public interface IBoardRepository : IGenericRepository<Board>
{
    Task<List<Board>> GetByWorkspaceIdAsync(int workspaceId);
}