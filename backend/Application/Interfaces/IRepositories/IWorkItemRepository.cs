public interface IWorkItemRepository : IGenericRepository<WorkItem>
{
    Task<List<WorkItem>> GetByBoardIdAsync(int boardId);
}