using backend.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public class MessageRepository: GenericRepository<Message>, IMessageRepository
{
    public MessageRepository(AppDbContext context): base(context){}

    public async Task<List<Message>> GetMessagesByConversationId(int conversationId)
    {
        return await _dbSet.Where(m => m.ConversationId == conversationId).ToListAsync();
    }



}