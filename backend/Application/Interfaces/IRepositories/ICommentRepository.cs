public interface ICommentRepository : IGenericRepository<Comment>
{
    Task<List<Comment>> GetByWorkItemIdAsync(int workItemId);
}