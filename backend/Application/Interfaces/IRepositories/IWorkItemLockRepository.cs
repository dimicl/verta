public interface IWorkItemLockRepository : IGenericRepository<WorkItemLock>
{
    Task<WorkItemLock?> GetByWorkItemIdAsync(int workItemId);
    Task<List<WorkItemLock>> GetExpiredAsync();
    Task<bool> TryRefreshActiveLockAsync(int workItemId, int userId, DateTime newExpiresAt);
}