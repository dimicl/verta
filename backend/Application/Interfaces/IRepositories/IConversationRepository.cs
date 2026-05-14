public interface IConversationRepository: IGenericRepository<Conversation>
{
    Task<Conversation?> GetDirectConversation(int senderId, int receiverId);

}