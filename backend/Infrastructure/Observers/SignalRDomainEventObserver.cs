using System.Text.Json;
using backend.API.Hubs;
using backend.Application.Interfaces;
using Microsoft.Extensions.Logging;

public class SignalRDomainEventObserver : IDomainEventObserver
{
    private static readonly HashSet<string> UserTargetedEvents = new(StringComparer.Ordinal)
    {
        DomainEventNames.WorkItemAssigned,
        DomainEventNames.WorkItemStatusChanged,
        DomainEventNames.YouNowHaveWriteAccess,
        DomainEventNames.WorkItemUnlocked,
        DomainEventNames.BoardInvitation,
        DomainEventNames.WorkspaceInvitation,
    };

    private static readonly HashSet<string> BoardBroadcastEvents = new(StringComparer.Ordinal)
    {
        DomainEventNames.WorkItemCreated,
        DomainEventNames.WorkItemUpdated,
        DomainEventNames.WorkItemDeleted,
        DomainEventNames.WorkItemPriorityChanged,
        DomainEventNames.CommentCreated,
        DomainEventNames.CommentUpdated,
        DomainEventNames.CommentDeleted,
        DomainEventNames.WorkItemFileAdded,
        DomainEventNames.WorkItemFileDeleted,
        DomainEventNames.BoardLocked,
        DomainEventNames.BoardUnlocked,
        DomainEventNames.BoardLockTransferred,
        DomainEventNames.SubWorkItemCreated,
        DomainEventNames.SubWorkItemUpdated,
        DomainEventNames.SubWorkItemStatusChanged,
        DomainEventNames.SubWorkItemDeleted,
    };

    private readonly INotificationService _notificationService;
    private readonly ILogger<SignalRDomainEventObserver> _logger;

    public SignalRDomainEventObserver(
        INotificationService notificationService,
        ILogger<SignalRDomainEventObserver> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task UpdateAsync(string eventName, object payload)
    {
        var isUserTargeted = UserTargetedEvents.Contains(eventName);
        var isBoardBroadcast = BoardBroadcastEvents.Contains(eventName);

        if (!isUserTargeted && !isBoardBroadcast)
            return;

        var element = JsonSerializer.SerializeToElement(payload);
        var clientEventName = eventName;

        if (element.TryGetProperty("ClientEventName", out var clientNameProp)
            && clientNameProp.ValueKind == JsonValueKind.String
            && !string.IsNullOrWhiteSpace(clientNameProp.GetString()))
        {
            clientEventName = clientNameProp.GetString()!;
        }

        if (isUserTargeted)
        {
            if (TryGetTargetUserIds(element, out var userIds))
            {
                foreach (var userId in userIds)
                {
                    await NotifyUserAsync(userId, clientEventName, payload);
                }
            }
            else if (element.TryGetProperty("TargetUserId", out var targetProp)
                && targetProp.TryGetInt32(out var targetUserId))
            {
                await NotifyUserAsync(targetUserId, clientEventName, payload);
            }
        }

        if (isBoardBroadcast
            && element.TryGetProperty("BoardId", out var boardProp)
            && boardProp.TryGetInt32(out var boardId)
            && boardId > 0)
        {
            await NotifyBoardAsync(boardId, clientEventName, payload);
        }
    }

    private static bool TryGetTargetUserIds(JsonElement element, out List<int> userIds)
    {
        userIds = [];

        if (!element.TryGetProperty("TargetUserIds", out var idsProp)
            || idsProp.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        foreach (var id in idsProp.EnumerateArray())
        {
            if (id.TryGetInt32(out var userId))
                userIds.Add(userId);
        }

        return userIds.Count > 0;
    }

    private async Task NotifyUserAsync(int userId, string clientEventName, object payload)
    {
        _logger.LogDebug(
            "SignalR domain event {EventName} -> user {UserId}",
            clientEventName,
            userId);

        await _notificationService.SendToUserAsync(userId, clientEventName, payload);
    }

    private async Task NotifyBoardAsync(int boardId, string clientEventName, object payload)
    {
        _logger.LogDebug(
            "SignalR domain event {EventName} -> board {BoardId}",
            clientEventName,
            boardId);

        await _notificationService.SendToGroupAsync(
            SystemHub.BoardGroup(boardId),
            clientEventName,
            payload);
    }
}
