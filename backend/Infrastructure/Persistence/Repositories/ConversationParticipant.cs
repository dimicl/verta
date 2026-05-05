using backend.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
public class ConversationParticipantRepository: GenericRepository<ConversationParticipant>, IConversationParticipantRepository
{
    public ConversationParticipantRepository(AppDbContext context): base(context){}
    public async Task<bool> IsParticipant(int conversationId, int userId)
    {
        return await _dbSet.AnyAsync(p => p.ConversationId == conversationId && p.UserId == userId);
    }
    public async Task<List<int>> GetUserIds(int conversationId)
    {
        return await _dbSet.Where(p => p.ConversationId == conversationId).Select(p => p.UserId).ToListAsync();
    }
}