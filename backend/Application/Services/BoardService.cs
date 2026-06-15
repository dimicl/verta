using backend.Application.Interfaces;
using backend.Shared.Helpers;

namespace backend.Application.Services;

public class BoardService : IBoardService
{
    private readonly IBoardRepository _boardRepo;
    private readonly IWorkspaceRepository _workspaceRepo;
    private readonly IWorkspaceMemberRepository _workspaceMemberRepo;
    private readonly IUserContext _userContext;

    public BoardService(
        IBoardRepository boardRepo,
        IWorkspaceRepository workspaceRepo,
        IWorkspaceMemberRepository workspaceMemberRepo,
        IUserContext userContext)
    {
        _boardRepo = boardRepo;
        _workspaceRepo = workspaceRepo;
        _workspaceMemberRepo = workspaceMemberRepo;
        _userContext = userContext;
    }

    public async Task<BoardResponse> Create(BoardRequest request)
    {
        if (request == null)
            throw new Exception("Request not found.");

        if (string.IsNullOrWhiteSpace(request.Name))
            throw new Exception("Board name is required.");

        var userId = _userContext.GetUserId();

        var workspace = await _workspaceRepo.GetById(request.WorkspaceId);
        if (workspace == null)
            throw new Exception("Workspace does not exist.");

        var member = await _workspaceMemberRepo.GetByWorkspaceAndUserIdAsync(
            request.WorkspaceId,
            userId
        );

        if (member == null)
            throw new Exception("You are not member of this workspace.");

        if (member.Role == UserRole.Guest)
            throw new Exception("Guest cannot create board.");

        var board = new Board
        {
            Name = request.Name,
            WorkspaceId = request.WorkspaceId,
            OwnerId = userId,
            CreatedAt = DateTime.UtcNow
        };

        var createdBoard = await _boardRepo.Add(board);

        return BoardHelper.ToResponse(createdBoard);
    }

    public async Task<List<BoardResponse>> GetByWorkspaceId(int workspaceId)
    {
        var userId = _userContext.GetUserId();

        var member = await _workspaceMemberRepo.GetByWorkspaceAndUserIdAsync(
            workspaceId,
            userId
        );

        if (member == null)
            throw new Exception("You are not member of this workspace.");

        var boards = await _boardRepo.GetByWorkspaceIdAsync(workspaceId);

        return boards.Select(BoardHelper.ToResponse).ToList();
    }

    public async Task<BoardResponse> GetById(int boardId)
    {
        var userId = _userContext.GetUserId();

        var board = await _boardRepo.GetById(boardId);
        if (board == null)
            throw new Exception("Board does not exist.");

        var member = await _workspaceMemberRepo.GetByWorkspaceAndUserIdAsync(
            board.WorkspaceId,
            userId
        );

        if (member == null)
            throw new Exception("You are not member of this workspace.");

        return BoardHelper.ToResponse(board);
    }
}