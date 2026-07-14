using backend.API.Hubs;
using backend.Application.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace backend.Infrastructure.Notifications;

public class SignalRNotificationService : INotificationService
{
    private readonly IHubContext<SystemHub> _hubContext;

    public SignalRNotificationService(IHubContext<SystemHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task SendChatMessageAsync(IEnumerable<int> userIds, Message message)
    {
        var groups = userIds.Select(SystemHub.UserGroup);
        return _hubContext.Clients.Groups(groups).SendAsync("ChatMessageReceived", message);
    }

    public Task SendToUserAsync(int userId, string eventName, object payload)
    {
        return _hubContext.Clients
            .Group(SystemHub.UserGroup(userId))
            .SendAsync(eventName, payload);
    }

    public Task SendToGroupAsync(string groupName, string eventName, object payload)
    {
        return _hubContext.Clients
            .Group(groupName)
            .SendAsync(eventName, payload);
    }
}
