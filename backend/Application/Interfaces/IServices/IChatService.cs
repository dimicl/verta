namespace backend.Application.Interfaces;

public interface IChatService
{
    Task<MessageResponse> SendMessage(int senderId, int receiverId, string content);
    Task<List<MessageResponse>> GetMessages(int conversationId, int userId, int? before, int limit);
    Task<bool> IsUserInConversation(int conversationId, int userId);
    Task MarkAsRead(int conversationId, int userId);
    Task<int> GetUnreadCount(int conversationId, int userId);
    Task<List<ConversationResponse>> GetMyConversations(int userId);

    Task<int> GetOrCreateDirectConversationId(int senderId, int receiverId);
}