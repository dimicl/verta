using backend.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public class MessageRepository : GenericRepository<Message>, IMessageRepository
{
    public MessageRepository(AppDbContext context) : base(context) { }

    public async Task<List<Message>> GetMessagesByConversationId(int conversationId, int? before, int limit)
    {
        var query = _dbSet.Where(m => m.ConversationId == conversationId);

        if (before.HasValue)
            query = query.Where(m => m.Id < before.Value);

        return await query
            .OrderByDescending(m => m.Id)
            .Take(limit)
            .OrderBy(m => m.Id)
            .ToListAsync();
    }

    public async Task<int?> GetLatestMessageIdAsync(int conversationId)
    {
        return await _dbSet
            .Where(m => m.ConversationId == conversationId)
            .OrderByDescending(m => m.Id)
            .Select(m => (int?)m.Id)
            .FirstOrDefaultAsync();
    }

    public async Task<int> GetUnreadCountAsync(int conversationId, int userId, int? lastReadMessageId)
    {
        var query = _dbSet.Where(m =>
            m.ConversationId == conversationId &&
            m.SenderId != userId);

        if (lastReadMessageId.HasValue)
            query = query.Where(m => m.Id > lastReadMessageId.Value);

        return await query.CountAsync();
    }
}