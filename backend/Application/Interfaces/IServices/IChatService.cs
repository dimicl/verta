namespace backend.Application.Interfaces;

public interface IChatService
{
    Task<Message> SendMessage(int senderId, int receiverId, string content);
    Task<List<Message>> GetMessages(int conversationId, int userId);
    Task<bool> IsUserInConversation(int conversationId, int userId);
}