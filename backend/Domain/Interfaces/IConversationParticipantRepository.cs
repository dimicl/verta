public interface IConversationParticipantRepository : IGenericRepository<ConversationParticipant>
{
    Task<bool> IsParticipant(int conversationId, int userId);
    Task<List<int>> GetUserIds(int conversationId);
}