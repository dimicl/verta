public interface IMessageRepository: IGenericRepository<Message>
{
    Task<List<Message>> GetMessagesByConversationId(int conversationId);
}