public interface IMessageRepository : IGenericRepository<Message>
{
    Task<List<Message>> GetMessagesByConversationId(int conversationId, int? before, int limit);
    Task<int?> GetLatestMessageIdAsync(int conversationId);
    Task<int> GetUnreadCountAsync(int conversationId, int userId, int? lastReadMessageId);
}