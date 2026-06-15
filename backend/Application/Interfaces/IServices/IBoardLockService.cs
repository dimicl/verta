namespace backend.Application.Interfaces;

public interface IBoardLockService
{
    Task<BoardLockResponse> OpenBoard(int boardId);
    Task<BoardLockResponse> CloseBoard(int boardId);
}