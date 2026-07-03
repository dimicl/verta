using backend.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public class BoardMemberRepository
    : GenericRepository<BoardMember>, IBoardMemberRepository
{
    public BoardMemberRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<BoardMember?> GetByBoardAndUserIdAsync(int boardId, int userId)
    {
        return await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.BoardId == boardId && x.UserId == userId);
    }

    public async Task<List<int>> GetBoardIdsByUserIdAsync(int userId, int workspaceId)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.Board!.WorkspaceId == workspaceId)
            .Select(x => x.BoardId)
            .ToListAsync();
    }

    public async Task<List<BoardMember>> GetByBoardIdWithUsersAsync(int boardId)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(x => x.User)
            .Where(x => x.BoardId == boardId)
            .ToListAsync();
    }
}
