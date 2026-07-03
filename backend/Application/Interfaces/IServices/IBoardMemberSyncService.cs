namespace backend.Application.Interfaces;

public interface IBoardMemberSyncService
{
    Task AddUserToAllWorkspaceBoardsAsync(int workspaceId, int userId);
    Task AddWorkspaceMembersToBoardAsync(int boardId, int workspaceId);
}
