using backend.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using backend.Application.Exceptions;

namespace backend.Application.Services;

public class BoardLockService : IBoardLockService, IBoardLockPromotionService
{
    private static readonly TimeSpan LockDuration = TimeSpan.FromSeconds(30);

    private readonly IBoardLockRepository _lockRepo;
    private readonly IBoardLockQueueRepository _queueRepo;
    private readonly IBoardRepository _boardRepo;
    private readonly IBoardAccessService _boardAccessService;
    private readonly IUserContext _userContext;
    private readonly DomainEventSubject _domainEventSubject;
    private readonly IUnitOfWork _unitOfWork;

    public BoardLockService(
        IBoardLockRepository lockRepo,
        IBoardLockQueueRepository queueRepo,
        IBoardRepository boardRepo,
        IBoardAccessService boardAccessService,
        IUserContext userContext,
        DomainEventSubject domainEventSubject,
        IUnitOfWork unitOfWork)
    {
        _lockRepo = lockRepo;
        _queueRepo = queueRepo;
        _boardRepo = boardRepo;
        _boardAccessService = boardAccessService;
        _userContext = userContext;
        _domainEventSubject = domainEventSubject;
        _unitOfWork = unitOfWork;
    }

    public async Task<BoardLockResponse> OpenBoard(int boardId)
    {
        var userId = _userContext.GetUserId();

        var board = await _boardRepo.GetById(boardId);
        if (board == null) throw new NotFoundException("Board does not exist.");

        try
        {
            await _boardAccessService.EnsureBoardAccessAsync(board);
        }
        catch (Exception ex)
        {
            throw new ForbiddenException(ex.Message);
        }

        var existingLock = await _lockRepo.GetByBoardIdAsync(boardId);
        if (existingLock != null && existingLock.ExpiresAt < DateTime.UtcNow)
        {
            try
            {
                await _lockRepo.Delete(existingLock);
            }
            catch (DbUpdateConcurrencyException)
            {
            }
            existingLock = null;
        }

        if (existingLock == null)
        {
            var now = DateTime.UtcNow;
            var boardLock = new BoardLock
            {
                BoardId = boardId,
                LockedByUserId = userId,
                LockedAt = now,
                ExpiresAt = now.Add(LockDuration)
            };

            try
            {
                var createdLock = await _lockRepo.Add(boardLock);
                await _domainEventSubject.NotifyAsync(DomainEventNames.BoardLocked,
                    new { BoardId = boardId, LockedByUserId = userId, Mode = "WRITE" });

                return new BoardLockResponse
                {
                    BoardId = boardId,
                    UserId = userId,
                    Mode = "WRITE",
                    LockedAt = createdLock.LockedAt,
                    ExpiresAt = createdLock.ExpiresAt
                };
            }
            catch (DbUpdateException)
            {
                existingLock = await _lockRepo.GetByBoardIdAsync(boardId);
                if (existingLock == null) throw; 
            }
        }

        if (existingLock.LockedByUserId == userId)
        {
            existingLock.ExpiresAt = DateTime.UtcNow.Add(LockDuration);
            await _lockRepo.Update(existingLock);

            return new BoardLockResponse
            {
                BoardId = boardId,
                UserId = userId,
                Mode = "WRITE",
                LockedAt = existingLock.LockedAt,
                ExpiresAt = existingLock.ExpiresAt
            };
        }

        await _queueRepo.EnqueueAsync(boardId, userId);
        var position = await _queueRepo.GetPositionAsync(boardId, userId);

        return new BoardLockResponse
        {
            BoardId = boardId,
            UserId = userId,
            Mode = "READ_ONLY",
            LockedAt = existingLock.LockedAt,
            ExpiresAt = existingLock.ExpiresAt,
            QueuePosition = position
        };
    }

    public async Task<BoardLockResponse> CloseBoard(int boardId)
    {
        var userId = _userContext.GetUserId();

        var existingLock = await _lockRepo.GetByBoardIdAsync(boardId);

        if (existingLock != null && existingLock.LockedByUserId == userId)
        {
            await _lockRepo.Delete(existingLock);
            await PromoteNextInQueueAsync(boardId);

            return new BoardLockResponse
            {
                BoardId = boardId,
                UserId = userId,
                Mode = "UNLOCKED",
                LockedAt = null,
                ExpiresAt = null
            };
        }

        await _queueRepo.RemoveUserAsync(boardId, userId);

        return new BoardLockResponse
        {
            BoardId = boardId,
            UserId = userId,
            Mode = "NO_LOCK",
            LockedAt = null,
            ExpiresAt = null
        };
    }

    public async Task<BoardLockResponse> Heartbeat(int boardId)
    {
        var userId = _userContext.GetUserId();

        var existingLock = await _lockRepo.GetByBoardIdAsync(boardId);

        if (existingLock == null || existingLock.LockedByUserId != userId)
            throw new ForbiddenException("You do not hold the lock for this board.");

        existingLock.ExpiresAt = DateTime.UtcNow.Add(LockDuration);
        await _lockRepo.Update(existingLock);

        return new BoardLockResponse
        {
            BoardId = boardId,
            UserId = userId,
            Mode = "WRITE",
            LockedAt = existingLock.LockedAt,
            ExpiresAt = existingLock.ExpiresAt
        };
    }

    public async Task PromoteNextInQueueAsync(int boardId)
    {
        var next = await _queueRepo.GetFirstAsync(boardId);

        if (next == null)
        {
            await _domainEventSubject.NotifyAsync(DomainEventNames.BoardUnlocked, new { BoardId = boardId });
            return;
        }

        var now = DateTime.UtcNow;
        var newLock = new BoardLock
        {
            BoardId = boardId,
            LockedByUserId = next.UserId,
            LockedAt = now,
            ExpiresAt = now.Add(LockDuration)
        };

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            await _lockRepo.Add(newLock);
            await _queueRepo.RemoveAsync(next);
            await _unitOfWork.CommitAsync();
        }
        catch (DbUpdateException)
        {
            await _unitOfWork.RollbackAsync();
            // Race condition: drugi poziv je već promovisao korisnika.
            return;
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }

        await _domainEventSubject.NotifyAsync(DomainEventNames.YouNowHaveWriteAccess, new
        {
            TargetUserId = next.UserId,
            BoardId = boardId,
            ExpiresAt = newLock.ExpiresAt
        });

        await _domainEventSubject.NotifyAsync(DomainEventNames.BoardLockTransferred, new
        {
            BoardId = boardId,
            NewLockedByUserId = next.UserId
        });
    }
}