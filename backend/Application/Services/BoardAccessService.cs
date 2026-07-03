using backend.Application.Interfaces;

namespace backend.Application.Services;

public class BoardAccessService : IBoardAccessService
{
    private readonly IWorkspaceMemberRepository _workspaceMemberRepo;
    private readonly IWorkspaceRepository _workspaceRepo;
    private readonly IBoardMemberRepository _boardMemberRepo;
    private readonly IUserContext _userContext;

    public BoardAccessService(
        IWorkspaceMemberRepository workspaceMemberRepo,
        IWorkspaceRepository workspaceRepo,
        IBoardMemberRepository boardMemberRepo,
        IUserContext userContext)
    {
        _workspaceMemberRepo = workspaceMemberRepo;
        _workspaceRepo = workspaceRepo;
        _boardMemberRepo = boardMemberRepo;
        _userContext = userContext;
    }

    public async Task<WorkspaceMember> EnsureBoardAccessAsync(Board board)
    {
        var userId = _userContext.GetUserId();

        var member = await _workspaceMemberRepo.GetByWorkspaceAndUserIdAsync(
            board.WorkspaceId,
            userId
        );

        if (member == null)
            throw new Exception("You are not member of this workspace.");

        if (await HasFullWorkspaceBoardAccessAsync(board.WorkspaceId, userId))
            return member;

        var boardMember = await _boardMemberRepo.GetByBoardAndUserIdAsync(board.Id, userId);
        if (boardMember == null)
            throw new Exception("You do not have access to this board.");

        return member;
    }

    public async Task<bool> HasFullWorkspaceBoardAccessAsync(int workspaceId, int userId)
    {
        var workspace = await _workspaceRepo.GetById(workspaceId);
        if (workspace == null)
            return false;

        if (workspace.OwnerId == userId)
            return true;

        var member = await _workspaceMemberRepo.GetByWorkspaceAndUserIdAsync(workspaceId, userId);
        if (member == null)
            return false;

        return member.Role is UserRole.Owner or UserRole.Member;
    }
}
