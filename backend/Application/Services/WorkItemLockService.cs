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
    private readonly IWorkspaceMemberRepository _workspaceMemberRepo;
    private readonly IUserContext _userContext;
    private readonly INotificationService _notificationService;

    public WorkItemLockService(
        IWorkItemLockRepository lockRepo,
        IWorkItemLockInterestRepository interestRepo,
        IWorkItemRepository workItemRepo,
        IBoardRepository boardRepo,
        IWorkspaceMemberRepository workspaceMemberRepo,
        IUserContext userContext,
        INotificationService notificationService)
    {
        _lockRepo = lockRepo;
        _interestRepo = interestRepo;
        _workItemRepo = workItemRepo;
        _boardRepo = boardRepo;
        _workspaceMemberRepo = workspaceMemberRepo;
        _userContext = userContext;
        _notificationService = notificationService;
    }

    public async Task<WorkItemLockResponse> OpenWorkItem(int workItemId)
    {
        var userId = _userContext.GetUserId();

        var workItem = await _workItemRepo.GetById(workItemId);
        if (workItem == null) throw new NotFoundException("Work item does not exist.");

        var board = await _boardRepo.GetById(workItem.BoardId);
        if (board == null) throw new NotFoundException("Board does not exist.");

        var member = await _workspaceMemberRepo.GetByWorkspaceAndUserIdAsync(board.WorkspaceId, userId);
        if (member == null) throw new ForbiddenException("You are not a member of this workspace.");

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

        return new WorkItemLockResponse
        {
            WorkItemId = workItemId,
            UserId = userId,
            Mode = "READ_ONLY",
            LockedAt = existingLock.LockedAt,
            ExpiresAt = existingLock.ExpiresAt
        };
    }

    public async Task<WorkItemLockResponse> CloseWorkItem(int workItemId)
    {
        var userId = _userContext.GetUserId();

        var existingLock = await _lockRepo.GetByWorkItemIdAsync(workItemId);

        if (existingLock != null && existingLock.LockedByUserId == userId)
        {
            await _lockRepo.Delete(existingLock);
            await NotifyAndClearInterestsAsync(workItemId);

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

    public async Task NotifyAndClearInterestsAsync(int workItemId)
    {
        var interestedUserIds = await _interestRepo.GetInterestedUserIdsAsync(workItemId);

        foreach (var interestedUserId in interestedUserIds)
        {
            await _notificationService.SendToUserAsync(interestedUserId, "WorkItemUnlocked", new
            {
                WorkItemId = workItemId
            });
        }

        await _interestRepo.RemoveAllAsync(workItemId);
    }
}