namespace backend.Application.Interfaces;

public interface IBoardService
{
    Task<BoardResponse> Create(BoardRequest request);
    Task<List<BoardResponse>> GetByWorkspaceId(int workspaceId);
    Task<BoardResponse> GetById(int boardId);
}