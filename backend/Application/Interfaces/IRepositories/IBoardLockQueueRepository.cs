public interface IBoardLockQueueRepository
{
    Task EnqueueAsync(int boardId, int userId);
    Task<BoardLockQueueEntry?> GetFirstAsync(int boardId);
    Task<int> GetPositionAsync(int boardId, int userId);
    Task RemoveAsync(BoardLockQueueEntry entry);
    Task RemoveUserAsync(int boardId, int userId);
}