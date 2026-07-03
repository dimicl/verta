namespace backend.Application.Interfaces;

public interface IWorkItemLockService
{
    Task<WorkItemLockResponse> OpenWorkItem(int workItemId);
    Task<WorkItemLockResponse> CloseWorkItem(int workItemId);
    Task<WorkItemLockResponse> Heartbeat(int workItemId);
    Task EnsureUserHasWriteLockAsync(int workItemId);
}