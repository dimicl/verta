public interface IWorkItemLockInterestRepository : IGenericRepository<WorkItemLockInterest>
{
    Task RegisterInterestAsync(int workItemId, int userId);
    Task<List<int>> GetInterestedUserIdsAsync(int workItemId);
    Task RemoveAllAsync(int workItemId);
    Task RemoveUserAsync(int workItemId, int userId);
    Task RemoveStaleAsync(TimeSpan olderThan);
}