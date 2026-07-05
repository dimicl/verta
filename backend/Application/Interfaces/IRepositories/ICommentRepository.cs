public interface ICommentRepository : IGenericRepository<Comment>
{
    Task<List<Comment>> GetByWorkItemIdAsync(int workItemId);
    Task<List<Comment>> GetBySubWorkItemIdAsync(int subWorkItemId);
    Task<Comment?> GetByIdWithUserAsync(int id);
}