public interface IWorkItemLockRepository : IGenericRepository<WorkItemLock>
{
    Task<WorkItemLock?> GetByWorkItemIdAsync(int workItemId);
    Task<List<WorkItemLock>> GetExpiredAsync();
}