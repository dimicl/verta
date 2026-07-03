public interface IConversationRepository: IGenericRepository<Conversation>
{
    Task<Conversation?> GetDirectConversation(int senderId, int receiverId);
    Task<List<Conversation>> GetByUserIdAsync(int userId);
    Task<Conversation?> GetByIdWithParticipants(int id);
}