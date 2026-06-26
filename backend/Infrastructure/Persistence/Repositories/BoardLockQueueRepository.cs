using backend.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public class BoardLockQueueRepository : IBoardLockQueueRepository
{
    private readonly AppDbContext _context;
    private readonly DbSet<BoardLockQueueEntry> _dbSet;

    public BoardLockQueueRepository(AppDbContext context)
    {
        _context = context;
        _dbSet = context.Set<BoardLockQueueEntry>();
    }

    public async Task EnqueueAsync(int boardId, int userId)
    {
        var exists = await _dbSet.AnyAsync(x => x.BoardId == boardId && x.UserId == userId);
        if (exists) return;

        try
        {
            await _dbSet.AddAsync(new BoardLockQueueEntry
            {
                BoardId = boardId,
                UserId = userId,
                JoinedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            _context.ChangeTracker.Clear();
        }
    }

    public async Task<BoardLockQueueEntry?> GetFirstAsync(int boardId)
    {
        return await _dbSet
            .Where(x => x.BoardId == boardId)
            .OrderBy(x => x.JoinedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<int> GetPositionAsync(int boardId, int userId)
    {
        var userEntry = await _dbSet
            .FirstOrDefaultAsync(x => x.BoardId == boardId && x.UserId == userId);

        if (userEntry == null) return 0;

        return await _dbSet
            .CountAsync(x => x.BoardId == boardId && x.JoinedAt <= userEntry.JoinedAt);
    }

    public async Task RemoveAsync(BoardLockQueueEntry entry)
    {
        _dbSet.Remove(entry);
        await _context.SaveChangesAsync();
    }

    public async Task RemoveUserAsync(int boardId, int userId)
    {
        var entry = await _dbSet
            .FirstOrDefaultAsync(x => x.BoardId == boardId && x.UserId == userId);

        if (entry == null) return;

        _dbSet.Remove(entry);
        await _context.SaveChangesAsync();
    }
}