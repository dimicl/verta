public interface ISubWorkItemRepository : IGenericRepository<SubWorkItem>
{
    Task<List<SubWorkItem>> GetByWorkItemIdAsync(int workItemId);
}