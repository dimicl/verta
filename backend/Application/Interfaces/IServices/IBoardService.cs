namespace backend.Application.Interfaces;

public interface IBoardService
{
    Task<BoardResponse> Create(BoardRequest request);
    Task<List<BoardResponse>> GetByWorkspaceId(int workspaceId);
    Task<BoardResponse> GetById(int boardId);
    Task InviteToBoard(BoardInviteRequest request);
    Task<List<WorkspaceMemberResponse>> GetMembersByBoardId(int boardId);
}
