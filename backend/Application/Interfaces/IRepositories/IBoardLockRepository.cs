public interface IBoardLockRepository : IGenericRepository<BoardLock>
{
    Task<BoardLock?> GetByBoardIdAsync(int boardId);
    Task<List<BoardLock>> GetExpiredAsync();
}