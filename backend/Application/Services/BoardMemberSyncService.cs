using backend.Application.Interfaces;

namespace backend.Application.Services;

public class BoardMemberSyncService : IBoardMemberSyncService
{
    private readonly IBoardRepository _boardRepo;
    private readonly IBoardMemberRepository _boardMemberRepo;
    private readonly IWorkspaceMemberRepository _workspaceMemberRepo;

    public BoardMemberSyncService(
        IBoardRepository boardRepo,
        IBoardMemberRepository boardMemberRepo,
        IWorkspaceMemberRepository workspaceMemberRepo)
    {
        _boardRepo = boardRepo;
        _boardMemberRepo = boardMemberRepo;
        _workspaceMemberRepo = workspaceMemberRepo;
    }

    public async Task AddUserToAllWorkspaceBoardsAsync(int workspaceId, int userId)
    {
        var boards = await _boardRepo.GetByWorkspaceIdAsync(workspaceId);

        foreach (var board in boards)
        {
            await EnsureBoardMemberAsync(board.Id, userId);
        }
    }

    public async Task AddWorkspaceMembersToBoardAsync(int boardId, int workspaceId)
    {
        var workspaceMembers = await _workspaceMemberRepo.GetByWorkspaceIdAsync(workspaceId);

        foreach (var member in workspaceMembers)
        {
            if (member.Role is UserRole.Owner or UserRole.Member)
            {
                await EnsureBoardMemberAsync(boardId, member.UserId);
            }
        }
    }

    private async Task EnsureBoardMemberAsync(int boardId, int userId)
    {
        var existing = await _boardMemberRepo.GetByBoardAndUserIdAsync(boardId, userId);
        if (existing != null)
            return;

        await _boardMemberRepo.Add(new BoardMember
        {
            BoardId = boardId,
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        });
    }
}
