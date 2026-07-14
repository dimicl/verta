using backend.Application.Interfaces;
using backend.Shared.Helpers;

using backend.Application.Exceptions;
namespace backend.Application.Services;

public class BoardService : IBoardService
{
    private readonly IBoardRepository _boardRepo;
    private readonly IWorkspaceRepository _workspaceRepo;
    private readonly IWorkspaceMemberRepository _workspaceMemberRepo;
    private readonly IBoardMemberRepository _boardMemberRepo;
    private readonly IBoardMemberSyncService _boardMemberSyncService;
    private readonly IBoardAccessService _boardAccessService;
    private readonly IUserRepository _userRepository;
    private readonly DomainEventSubject _domainEventSubject;
    private readonly IUserContext _userContext;

    public BoardService(
        IBoardRepository boardRepo,
        IWorkspaceRepository workspaceRepo,
        IWorkspaceMemberRepository workspaceMemberRepo,
        IBoardMemberRepository boardMemberRepo,
        IBoardMemberSyncService boardMemberSyncService,
        IBoardAccessService boardAccessService,
        IUserRepository userRepository,
        DomainEventSubject domainEventSubject,
        IUserContext userContext)
    {
        _boardRepo = boardRepo;
        _workspaceRepo = workspaceRepo;
        _workspaceMemberRepo = workspaceMemberRepo;
        _boardMemberRepo = boardMemberRepo;
        _boardMemberSyncService = boardMemberSyncService;
        _boardAccessService = boardAccessService;
        _userRepository = userRepository;
        _domainEventSubject = domainEventSubject;
        _userContext = userContext;
    }

    public async Task<BoardResponse> Create(BoardRequest request)
    {
        if (request == null)
            throw new ValidationException("Request not found.");

        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ValidationException("Board name is required.");

        var userId = _userContext.GetUserId();

        var workspace = await _workspaceRepo.GetById(request.WorkspaceId);
        if (workspace == null)
            throw new NotFoundException("Workspace does not exist.");

        if (!await _boardAccessService.HasFullWorkspaceBoardAccessAsync(request.WorkspaceId, userId))
            throw new ForbiddenException("You are not allowed to create board.");

        var board = new Board
        {
            Name = request.Name,
            WorkspaceId = request.WorkspaceId,
            OwnerId = userId,
            CreatedAt = DateTime.UtcNow
        };

        var createdBoard = await _boardRepo.Add(board);

        await _boardMemberSyncService.AddWorkspaceMembersToBoardAsync(
            createdBoard.Id,
            request.WorkspaceId
        );

        if (request.InviteEmails is { Count: > 0 })
        {
            foreach (var email in request.InviteEmails)
            {
                if (string.IsNullOrWhiteSpace(email))
                    continue;

                await AddUserToBoardAsync(createdBoard, email.Trim(), userId);
            }
        }

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
            throw new ForbiddenException("You are not member of this workspace.");

        var boards = await _boardRepo.GetByWorkspaceIdAsync(workspaceId);

        if (await _boardAccessService.HasFullWorkspaceBoardAccessAsync(workspaceId, userId))
            return boards.Select(BoardHelper.ToResponse).ToList();

        var accessibleBoardIds = await _boardMemberRepo.GetBoardIdsByUserIdAsync(userId, workspaceId);

        return boards
            .Where(b => accessibleBoardIds.Contains(b.Id))
            .Select(BoardHelper.ToResponse)
            .ToList();
    }

    public async Task<BoardResponse> GetById(int boardId)
    {
        var board = await _boardRepo.GetById(boardId);
        if (board == null)
            throw new NotFoundException("Board does not exist.");

        await _boardAccessService.EnsureBoardAccessAsync(board);

        return BoardHelper.ToResponse(board);
    }

    public async Task InviteToBoard(BoardInviteRequest request)
    {
        if (request == null)
            throw new ValidationException("Request not found.");

        if (string.IsNullOrWhiteSpace(request.Email))
            throw new ValidationException("Email is required.");

        var currentUserId = _userContext.GetUserId();

        var board = await _boardRepo.GetById(request.BoardId);
        if (board == null)
            throw new NotFoundException("Board does not exist.");

        var workspace = await _workspaceRepo.GetById(board.WorkspaceId);
        if (workspace == null)
            throw new NotFoundException("Workspace does not exist.");

        if (workspace.OwnerId != currentUserId && board.OwnerId != currentUserId)
            throw new ForbiddenException("Only workspace or board owner can invite to board.");

        await AddUserToBoardAsync(board, request.Email.Trim(), currentUserId);
    }

    public async Task<List<WorkspaceMemberResponse>> GetMembersByBoardId(int boardId)
    {
        var board = await _boardRepo.GetById(boardId);
        if (board == null)
            throw new NotFoundException("Board does not exist.");

        await _boardAccessService.EnsureBoardAccessAsync(board);
        await _boardMemberSyncService.AddWorkspaceMembersToBoardAsync(
            boardId,
            board.WorkspaceId
        );

        var responses = new List<WorkspaceMemberResponse>();
        var addedUserIds = new HashSet<int>();

        var workspaceMembers = await _workspaceMemberRepo.GetByWorkspaceIdAsync(board.WorkspaceId);
        foreach (var member in workspaceMembers)
        {
            if (member.User == null
                || member.Role == UserRole.Guest
                || !addedUserIds.Add(member.UserId))
                continue;

            responses.Add(new WorkspaceMemberResponse
            {
                Id = member.Id,
                WorkspaceId = board.WorkspaceId,
                UserId = member.UserId,
                Role = member.Role,
                CreatedAt = member.CreatedAt,
                FirstName = member.User.FirstName ?? string.Empty,
                LastName = member.User.LastName ?? string.Empty,
                IsOnline = member.User.IsOnline
            });
        }

        var boardMembers = await _boardMemberRepo.GetByBoardIdWithUsersAsync(boardId);
        foreach (var member in boardMembers)
        {
            if (member.User == null || !addedUserIds.Add(member.UserId))
                continue;

            responses.Add(new WorkspaceMemberResponse
            {
                Id = member.Id,
                WorkspaceId = board.WorkspaceId,
                UserId = member.UserId,
                Role = UserRole.Guest,
                CreatedAt = member.CreatedAt,
                FirstName = member.User.FirstName ?? string.Empty,
                LastName = member.User.LastName ?? string.Empty,
                IsOnline = member.User.IsOnline
            });
        }

        if (!addedUserIds.Contains(board.OwnerId))
        {
            var owner = await _userRepository.GetById(board.OwnerId);
            if (owner != null)
            {
                var ownerMembership = await _workspaceMemberRepo.GetByWorkspaceAndUserIdAsync(
                    board.WorkspaceId,
                    board.OwnerId
                );

                responses.Insert(0, new WorkspaceMemberResponse
                {
                    Id = ownerMembership?.Id ?? 0,
                    WorkspaceId = board.WorkspaceId,
                    UserId = owner.Id,
                    Role = ownerMembership?.Role ?? UserRole.Owner,
                    CreatedAt = ownerMembership?.CreatedAt ?? board.CreatedAt,
                    FirstName = owner.FirstName ?? string.Empty,
                    LastName = owner.LastName ?? string.Empty,
                    IsOnline = owner.IsOnline
                });
            }
        }

        return responses
            .OrderBy(r => r.FirstName)
            .ThenBy(r => r.LastName)
            .ToList();
    }

    private async Task AddUserToBoardAsync(Board board, string email, int invitedByUserId)
    {
        var invitedUser = await _userRepository.GetByEmailAsync(email);
        if (invitedUser == null)
            throw new NotFoundException("Invited user does not exist.");

        if (invitedUser.Id == invitedByUserId)
            throw new ForbiddenException("You cannot invite yourself.");

        var existingMember = await _workspaceMemberRepo.GetByWorkspaceAndUserIdAsync(
            board.WorkspaceId,
            invitedUser.Id
        );

        if (existingMember is { Role: UserRole.Owner or UserRole.Member })
            throw new ValidationException("User already has access to all boards in this workspace.");

        if (existingMember == null)
        {
            await _workspaceMemberRepo.Add(new WorkspaceMember
            {
                WorkspaceId = board.WorkspaceId,
                UserId = invitedUser.Id,
                Role = UserRole.Guest,
                CreatedAt = DateTime.UtcNow
            });
        }

        var existingBoardMember = await _boardMemberRepo.GetByBoardAndUserIdAsync(
            board.Id,
            invitedUser.Id
        );

        if (existingBoardMember != null)
            throw new ValidationException("User is already invited to this board.");

        await _boardMemberRepo.Add(new BoardMember
        {
            BoardId = board.Id,
            UserId = invitedUser.Id,
            CreatedAt = DateTime.UtcNow
        });

        var workspace = await _workspaceRepo.GetById(board.WorkspaceId);

        await _domainEventSubject.NotifyAsync(DomainEventNames.BoardInvitation, new
        {
            TargetUserId = invitedUser.Id,
            BoardId = board.Id,
            BoardName = board.Name,
            WorkspaceId = board.WorkspaceId,
            WorkspaceName = workspace?.Name,
            InvitedByUserId = invitedByUserId
        });
    }
}
