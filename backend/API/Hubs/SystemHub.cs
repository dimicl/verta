using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using backend.Application.Exceptions;
using backend.Application.Interfaces;

namespace backend.API.Hubs;

[Authorize]
public class SystemHub : Hub
{
    private readonly IUserRepository _userRepository;
    private readonly IBoardRepository _boardRepository;
    private readonly IBoardAccessService _boardAccessService;

    public SystemHub(
        IUserRepository userRepository,
        IBoardRepository boardRepository,
        IBoardAccessService boardAccessService)
    {
        _userRepository = userRepository;
        _boardRepository = boardRepository;
        _boardAccessService = boardAccessService;
    }

    public static string UserGroup(int userId) => $"user-{userId}";

    public static string BoardGroup(int boardId) => $"board-{boardId}";

    public async Task JoinBoard(int boardId)
    {
        if (boardId <= 0)
            return;

        var board = await _boardRepository.GetById(boardId);
        if (board == null)
            throw new HubException("Board does not exist.");

        try
        {
            await _boardAccessService.EnsureBoardAccessAsync(board);
        }
        catch (ForbiddenException ex)
        {
            throw new HubException(ex.Message);
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, BoardGroup(boardId));
    }

    public async Task LeaveBoard(int boardId)
    {
        if (boardId <= 0)
            return;

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, BoardGroup(boardId));
    }

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
            await Clients.Others.SendAsync("UserPresenceChanged", new
            {
                UserId = userId.Value,
                IsOnline = true
            });
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

            await Clients.Others.SendAsync("UserPresenceChanged", new
            {
                UserId = userId.Value,
                IsOnline = false
            });
        }
        await base.OnDisconnectedAsync(exception);
    }

    private int? GetUserId()
    {
        var value = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(value, out var id) ? id : null;
    }
}
