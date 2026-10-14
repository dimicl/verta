namespace backend.Application.Interfaces;

public interface IBoardLockService
{
    Task<BoardLockResponse> OpenBoard(int boardId);
    Task<BoardLockResponse> CloseBoard(int boardId);
    Task<BoardLockResponse> Heartbeat(int boardId);
    /// <summary>
    /// Allows mutation when no board lock exists.
    /// Blocks only when another user currently holds the board write lock.
    /// </summary>
    Task EnsureBoardAllowsMutationAsync(int boardId);
}