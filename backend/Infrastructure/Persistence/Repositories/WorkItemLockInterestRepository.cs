using backend.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public class WorkItemLockInterestRepository : GenericRepository<WorkItemLockInterest>, IWorkItemLockInterestRepository
{
    private readonly AppDbContext _context;

    public WorkItemLockInterestRepository(AppDbContext context) : base(context)
    {
        _context = context;
    }

    public async Task RegisterInterestAsync(int workItemId, int userId)
    {
        var exists = await _dbSet.AnyAsync(x => x.WorkItemId == workItemId && x.UserId == userId);
        if (exists) return;

        try
        {
            await _dbSet.AddAsync(new WorkItemLockInterest
            {
                WorkItemId = workItemId,
                UserId = userId,
                RegisteredAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            _context.ChangeTracker.Clear();
        }
    }

    public async Task<WorkItemLockInterest?> GetFirstAsync(int workItemId)
    {
        return await _dbSet
            .Where(x => x.WorkItemId == workItemId)
            .OrderBy(x => x.RegisteredAt)
            .FirstOrDefaultAsync();
    }

    public async Task<int> GetPositionAsync(int workItemId, int userId)
    {
        var userEntry = await _dbSet
            .FirstOrDefaultAsync(x => x.WorkItemId == workItemId && x.UserId == userId);

        if (userEntry == null) return 0;

        return await _dbSet
            .CountAsync(x => x.WorkItemId == workItemId && x.RegisteredAt <= userEntry.RegisteredAt);
    }

    public async Task<List<int>> GetInterestedUserIdsAsync(int workItemId)
    {
        return await _dbSet
            .Where(x => x.WorkItemId == workItemId)
            .Select(x => x.UserId)
            .ToListAsync();
    }

    public async Task RemoveAllAsync(int workItemId)
    {
        var entries = await _dbSet.Where(x => x.WorkItemId == workItemId).ToListAsync();
        _dbSet.RemoveRange(entries);
        await _context.SaveChangesAsync();
    }

    public async Task RemoveEntryAsync(WorkItemLockInterest entry)
    {
        _dbSet.Remove(entry);
        await _context.SaveChangesAsync();
    }

    public async Task RemoveUserAsync(int workItemId, int userId)
    {
        var entry = await _dbSet.FirstOrDefaultAsync(x => x.WorkItemId == workItemId && x.UserId == userId);
        if (entry == null) return;
        _dbSet.Remove(entry);
        await _context.SaveChangesAsync();
    }

    public async Task RemoveStaleAsync(TimeSpan olderThan)
    {
        var cutoff = DateTime.UtcNow - olderThan;
        var stale = await _dbSet.Where(x => x.RegisteredAt < cutoff).ToListAsync();
        if (stale.Count == 0) return;
        _dbSet.RemoveRange(stale);
        await _context.SaveChangesAsync();
    }
}