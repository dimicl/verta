public interface IBoardMemberRepository : IGenericRepository<BoardMember>
{
    Task<BoardMember?> GetByBoardAndUserIdAsync(int boardId, int userId);
    Task<List<int>> GetBoardIdsByUserIdAsync(int userId, int workspaceId);
    Task<List<BoardMember>> GetByBoardIdWithUsersAsync(int boardId);
}