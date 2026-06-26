using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace backend.API.Hubs;

[Authorize]
public class SystemHub : Hub
{
    private readonly IUserRepository _userRepository;

    public SystemHub(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public static string UserGroup(int userId) => $"user-{userId}";

    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        if (userId.HasValue)
        {
            var user = await _userRepository.GetById(userId.Value);
            if (user != null)
            {
                user.IsOnline = true;
                user.LastSeenAt = DateTime.UtcNow;
                await _userRepository.Update(user);
            }
            await Groups.AddToGroupAsync(Context.ConnectionId, UserGroup(userId.Value));
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();
        if (userId.HasValue)
        {
            var user = await _userRepository.GetById(userId.Value);
            if (user != null)
            {
                user.IsOnline = false;
                user.LastSeenAt = DateTime.UtcNow;
                await _userRepository.Update(user);
            }
        }
        await base.OnDisconnectedAsync(exception);
    }

    private int? GetUserId()
    {
        var value = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(value, out var id) ? id : null;
    }
}