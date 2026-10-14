namespace backend.Application.Interfaces;

public interface IWorkItemLockService
{
    Task<WorkItemLockResponse> OpenWorkItem(int workItemId);
    Task<WorkItemLockResponse> CloseWorkItem(int workItemId);
    Task<WorkItemLockResponse> Heartbeat(int workItemId);
    Task EnsureUserHasWriteLockAsync(int workItemId);
    /// <summary>
    /// Allows mutation when no lock exists (e.g. kanban drag).
    /// Blocks only when another user currently holds the write lock.
    /// </summary>
    Task EnsureWorkItemNotLockedByOtherAsync(int workItemId);
}
