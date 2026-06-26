namespace backend.Application.Interfaces;

public interface INotificationService
{
    Task SendChatMessageAsync(IEnumerable<int> userIds, Message message);
    Task SendToUserAsync(int userId, string eventName, object payload);
}
