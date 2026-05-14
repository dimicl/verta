namespace backend.Application.Interfaces;

public interface INotificationService
{
    Task SendUpdateAsync(string message);
    Task SendChatMessageAsync(IEnumerable<int> userIds, Message message);
}
