using backend.Application.Exceptions;
using backend.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace backend.Application.Services;

public class WorkItemLockService : IWorkItemLockService, IWorkItemLockExpiryService
{
    private static readonly TimeSpan LockDuration = TimeSpan.FromSeconds(30);

    private readonly IWorkItemLockRepository _lockRepo;
    private readonly IWorkItemLockInterestRepository _interestRepo;
    private readonly IWorkItemRepository _workItemRepo;
    private readonly IBoardRepository _boardRepo;
    private readonly IBoardAccessService _boardAccessService;
    private readonly IUserContext _userContext;
    private readonly DomainEventSubject _domainEventSubject;
    private readonly IUnitOfWork _unitOfWork;

    public WorkItemLockService(
        IWorkItemLockRepository lockRepo,
        IWorkItemLockInterestRepository interestRepo,
        IWorkItemRepository workItemRepo,
        IBoardRepository boardRepo,
        IBoardAccessService boardAccessService,
        IUserContext userContext,
        DomainEventSubject domainEventSubject,
        IUnitOfWork unitOfWork)
    {
        _lockRepo = lockRepo;
        _interestRepo = interestRepo;
        _workItemRepo = workItemRepo;
        _boardRepo = boardRepo;
        _boardAccessService = boardAccessService;
        _userContext = userContext;
        _domainEventSubject = domainEventSubject;
        _unitOfWork = unitOfWork;
    }

    public async Task<WorkItemLockResponse> OpenWorkItem(int workItemId)
    {
        var userId = _userContext.GetUserId();

        var workItem = await _workItemRepo.GetById(workItemId);
        if (workItem == null) throw new NotFoundException("Work item does not exist.");

        var board = await _boardRepo.GetById(workItem.BoardId);
        if (board == null) throw new NotFoundException("Board does not exist.");

        try
        {
            await _boardAccessService.EnsureBoardAccessAsync(board);
        }
        catch (Exception ex)
        {
            throw new ForbiddenException(ex.Message);
        }

        var existingLock = await _lockRepo.GetByWorkItemIdAsync(workItemId);

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
            var workItemLock = new WorkItemLock
            {
                WorkItemId = workItemId,
                LockedByUserId = userId,
                LockedAt = now,
                ExpiresAt = now.Add(LockDuration)
            };

            try
            {
                var created = await _lockRepo.Add(workItemLock);
                await _domainEventSubject.NotifyAsync(DomainEventNames.WorkItemLocked, new
                {
                    WorkItemId = workItemId,
                    LockedByUserId = userId,
                    Mode = "WRITE"
                });

                return new WorkItemLockResponse
                {
                    WorkItemId = workItemId,
                    UserId = userId,
                    Mode = "WRITE",
                    LockedAt = created.LockedAt,
                    ExpiresAt = created.ExpiresAt
                };
            }
            catch (DbUpdateException)
            {
                existingLock = await _lockRepo.GetByWorkItemIdAsync(workItemId);
                if (existingLock == null) throw;
            }
        }

        if (existingLock.LockedByUserId == userId)
        {
            existingLock.ExpiresAt = DateTime.UtcNow.Add(LockDuration);
            await _lockRepo.Update(existingLock);
            return new WorkItemLockResponse
            {
                WorkItemId = workItemId,
                UserId = userId,
                Mode = "WRITE",
                LockedAt = existingLock.LockedAt,
                ExpiresAt = existingLock.ExpiresAt
            };
        }

        await _interestRepo.RegisterInterestAsync(workItemId, userId);
        var position = await _interestRepo.GetPositionAsync(workItemId, userId);

        return new WorkItemLockResponse
        {
            WorkItemId = workItemId,
            UserId = userId,
            Mode = "READ_ONLY",
            LockedAt = existingLock.LockedAt,
            ExpiresAt = existingLock.ExpiresAt,
            QueuePosition = position
        };
    }

    public async Task<WorkItemLockResponse> CloseWorkItem(int workItemId)
    {
        var userId = _userContext.GetUserId();

        var existingLock = await _lockRepo.GetByWorkItemIdAsync(workItemId);

        if (existingLock != null && existingLock.LockedByUserId == userId)
        {
            await _lockRepo.Delete(existingLock);
            await PromoteNextInterestedAsync(workItemId);

            return new WorkItemLockResponse
            {
                WorkItemId = workItemId,
                UserId = userId,
                Mode = "UNLOCKED",
                LockedAt = null,
                ExpiresAt = null
            };
        }

        await _interestRepo.RemoveUserAsync(workItemId, userId);

        return new WorkItemLockResponse
        {
            WorkItemId = workItemId,
            UserId = userId,
            Mode = "NO_LOCK",
            LockedAt = null,
            ExpiresAt = null
        };
    }

    public async Task<WorkItemLockResponse> Heartbeat(int workItemId)
    {
        var userId = _userContext.GetUserId();

        var existingLock = await _lockRepo.GetByWorkItemIdAsync(workItemId);
        if (existingLock == null || existingLock.LockedByUserId != userId)
            throw new ForbiddenException("You do not hold the lock for this work item.");

        existingLock.ExpiresAt = DateTime.UtcNow.Add(LockDuration);
        await _lockRepo.Update(existingLock);

        return new WorkItemLockResponse
        {
            WorkItemId = workItemId,
            UserId = userId,
            Mode = "WRITE",
            LockedAt = existingLock.LockedAt,
            ExpiresAt = existingLock.ExpiresAt
        };
    }

    public async Task EnsureUserHasWriteLockAsync(int workItemId)
    {
        var userId = _userContext.GetUserId();
        var existingLock = await _lockRepo.GetByWorkItemIdAsync(workItemId);

        if (existingLock == null || existingLock.ExpiresAt < DateTime.UtcNow)
            throw new ForbiddenException("You must open the work item for editing before making changes.");

        if (existingLock.LockedByUserId != userId)
            throw new ForbiddenException("This work item is locked by another user.");
    }

    public async Task PromoteNextInterestedAsync(int workItemId)
    {
        var next = await _interestRepo.GetFirstAsync(workItemId);

        if (next == null)
        {
            var interestedUserIds = await _interestRepo.GetInterestedUserIdsAsync(workItemId);

            await _domainEventSubject.NotifyAsync(DomainEventNames.WorkItemUnlocked, new
            {
                WorkItemId = workItemId,
                TargetUserIds = interestedUserIds
            });
            return;
        }

        var now = DateTime.UtcNow;
        var newLock = new WorkItemLock
        {
            WorkItemId = workItemId,
            LockedByUserId = next.UserId,
            LockedAt = now,
            ExpiresAt = now.Add(LockDuration)
        };

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            await _lockRepo.Add(newLock);
            await _interestRepo.RemoveEntryAsync(next);
            await _unitOfWork.CommitAsync();
        }
        catch (DbUpdateException)
        {
            await _unitOfWork.RollbackAsync();
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
            WorkItemId = workItemId,
            ExpiresAt = newLock.ExpiresAt
        });

        await _domainEventSubject.NotifyAsync(DomainEventNames.WorkItemLockTransferred, new
        {
            WorkItemId = workItemId,
            NewLockedByUserId = next.UserId
        });
    }
}
