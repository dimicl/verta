using backend.API.Hubs;
using backend.Application.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace backend.Infrastructure.Notifications;

public class SignalRNotificationService : IChatService
{
    private readonly IHubContext<SystemHub> _hubContext;

    public SignalRNotificationService(IHubContext<SystemHub> hubContext)
    {
        _hubContext = hubContext;
    }
    public Task<Message> SendMessage(int conversationId, int senderId, string content)
    {
        return Task.FromResult(new Message { Id = 1, Content = content, SenderId = senderId, ConversationId = conversationId, CreatedAt = DateTime.Now });
    }

    public Task<bool> IsUserInConversation(int conversationId, int userId)
    {
        return Task.FromResult(true);
    }

    public Task<List<Message>> GetMessages(int conversationId)
    {
        return Task.FromResult(new List<Message> { new Message { Id = 1, Content = "Hello", SenderId = 1, ConversationId = conversationId, CreatedAt = DateTime.Now } });
    }

    public Task SendUpdateAsync(string message)
    {
        return _hubContext.Clients.All.SendAsync("SystemUpdate", message);
    }
}
