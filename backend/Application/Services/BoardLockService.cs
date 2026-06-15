using backend.Application.Interfaces;

namespace backend.Application.Services;

public class BoardLockService : IBoardLockService
{
    private readonly IBoardLockRepository _lockRepo;
    private readonly IBoardRepository _boardRepo;
    private readonly IWorkspaceMemberRepository _workspaceMemberRepo;
    private readonly IUserContext _userContext;
    private readonly DomainEventSubject _domainEventSubject;

    public BoardLockService(
        IBoardLockRepository lockRepo,
        IBoardRepository boardRepo,
        IWorkspaceMemberRepository workspaceMemberRepo,
        IUserContext userContext,
        DomainEventSubject domainEventSubject,
        SignalRDomainEventObserver signalRObserver,
        RabbitMqDomainEventObserver rabbitMqObserver)
    {
        _lockRepo = lockRepo;
        _boardRepo = boardRepo;
        _workspaceMemberRepo = workspaceMemberRepo;
        _userContext = userContext;
        _domainEventSubject = domainEventSubject;

        _domainEventSubject.Attach(signalRObserver);
        _domainEventSubject.Attach(rabbitMqObserver);
    }

    public async Task<BoardLockResponse> OpenBoard(int boardId)
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

        var existingLock = await _lockRepo.GetByBoardIdAsync(boardId);

        if (existingLock == null)
        {
            var boardLock = new BoardLock
            {
                BoardId = boardId,
                LockedByUserId = userId,
                LockedAt = DateTime.UtcNow
            };

            var createdLock = await _lockRepo.Add(boardLock);

            await _domainEventSubject.NotifyAsync("BoardLocked", new
            {
                BoardId = boardId,
                LockedByUserId = userId,
                Mode = "WRITE"
            });

            return new BoardLockResponse
            {
                BoardId = boardId,
                UserId = userId,
                Mode = "WRITE",
                LockedAt = createdLock.LockedAt
            };
        }

        if (existingLock.LockedByUserId == userId)
        {
            return new BoardLockResponse
            {
                BoardId = boardId,
                UserId = userId,
                Mode = "WRITE",
                LockedAt = existingLock.LockedAt
            };
        }

        return new BoardLockResponse
        {
            BoardId = boardId,
            UserId = userId,
            Mode = "READ_ONLY",
            LockedAt = existingLock.LockedAt
        };
    }

    public async Task<BoardLockResponse> CloseBoard(int boardId)
    {
        var userId = _userContext.GetUserId();

        var existingLock = await _lockRepo.GetByBoardIdAsync(boardId);

        if (existingLock == null)
        {
            return new BoardLockResponse
            {
                BoardId = boardId,
                UserId = userId,
                Mode = "NO_LOCK",
                LockedAt = null
            };
        }

        if (existingLock.LockedByUserId != userId)
        {
            return new BoardLockResponse
            {
                BoardId = boardId,
                UserId = userId,
                Mode = "READ_ONLY",
                LockedAt = existingLock.LockedAt
            };
        }

        await _lockRepo.Delete(existingLock);

        await _domainEventSubject.NotifyAsync("BoardUnlocked", new
        {
            BoardId = boardId,
            UnlockedByUserId = userId
        });

        return new BoardLockResponse
        {
            BoardId = boardId,
            UserId = userId,
            Mode = "UNLOCKED",
            LockedAt = null
        };
    }
}