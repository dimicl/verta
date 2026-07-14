namespace backend.Application.Interfaces;

public interface INotificationService
{
    Task SendChatMessageAsync(IEnumerable<int> userIds, Message message);
    Task SendToUserAsync(int userId, string eventName, object payload);
    Task SendToGroupAsync(string groupName, string eventName, object payload);
}
