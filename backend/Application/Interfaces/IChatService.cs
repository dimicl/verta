namespace backend.Application.Interfaces;

public interface IChatService
{
    Task<Message> SendMessage(int conversationId, int senderId, string content);
    Task<List<Message>> GetMessages(int conversationId);
    Task<bool> IsUserInConversation(int conversationId, int userId);
}