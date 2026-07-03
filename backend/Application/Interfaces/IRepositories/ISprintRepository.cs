public interface ISprintRepository : IGenericRepository<Sprint>
{
    Task<List<Sprint>> GetByBoardIdAsync(int boardId);
}
