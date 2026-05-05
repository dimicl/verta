using Microsoft.AspNetCore.SignalR;

namespace backend.API.Hubs;

public class SystemHub : Hub
{
    public static string UserGroup(int userId) => $"user-{userId}";

    public Task JoinUserGroup(int userId)
    {
        return Groups.AddToGroupAsync(Context.ConnectionId, UserGroup(userId));
    }

    public Task LeaveUserGroup(int userId)
    {
        return Groups.RemoveFromGroupAsync(Context.ConnectionId, UserGroup(userId));
    }

    public override async Task OnConnectedAsync()
    {
       await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
    }
}
