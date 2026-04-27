using Microsoft.AspNetCore.SignalR;

namespace backend.API.Hubs;

public class SystemHub : Hub
{
    public override async Task OnConnectedAsync()
    {
       await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
    }
}
