using backend.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
public class ConversationRepository: GenericRepository<Conversation>, IConversationRepository
{
    public ConversationRepository(AppDbContext context): base(context){}
    public async Task<Conversation?> GetDirectConversation(int senderId, int receiverId)
    {
        return await _dbSet
            .Include(c => c.Participants)
            .FirstOrDefaultAsync(c =>
                c.Type == ConversationType.Direct &&
                c.Participants.Any(p => p.UserId == senderId) &&
                c.Participants.Any(p => p.UserId == receiverId));
    }

    public async Task<List<Conversation>> GetByUserIdAsync(int userId)
    {
        return await _dbSet
            .Include(c => c.Participants)
            .Where(c => c.Participants.Any(p => p.UserId == userId))
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }
}