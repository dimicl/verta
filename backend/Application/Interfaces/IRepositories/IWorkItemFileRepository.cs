public interface IWorkItemFileRepository : IGenericRepository<WorkItemFile>
{
    Task<List<WorkItemFile>> GetByWorkItemIdAsync(int workItemId);
    Task<List<WorkItemFile>> GetBySubWorkItemIdAsync(int subWorkItemId);
}