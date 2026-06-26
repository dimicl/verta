using backend.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public class BoardLockRepository
    : GenericRepository<BoardLock>, IBoardLockRepository
{
    public BoardLockRepository(AppDbContext context)
        : base(context)
    {
    }

    public async Task<BoardLock?> GetByBoardIdAsync(int boardId)
    {
        return await _dbSet
            .FirstOrDefaultAsync(x => x.BoardId == boardId);
    }

    public async Task<List<BoardLock>> GetExpiredAsync()
    {
        var now = DateTime.UtcNow;
        return await _dbSet
            .Where(x => x.ExpiresAt < now)
            .ToListAsync();
    }
}