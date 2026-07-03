using System.Text.Json;
using backend.API.Hubs;
using Microsoft.AspNetCore.SignalR;
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

    private readonly IHubContext<SystemHub> _hubContext;
    private readonly ILogger<SignalRDomainEventObserver> _logger;

    public SignalRDomainEventObserver(
        IHubContext<SystemHub> hubContext,
        ILogger<SignalRDomainEventObserver> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task UpdateAsync(string eventName, object payload)
    {
        if (!UserTargetedEvents.Contains(eventName))
            return;

        var element = JsonSerializer.SerializeToElement(payload);
        var clientEventName = eventName;

        if (element.TryGetProperty("ClientEventName", out var clientNameProp)
            && clientNameProp.ValueKind == JsonValueKind.String
            && !string.IsNullOrWhiteSpace(clientNameProp.GetString()))
        {
            clientEventName = clientNameProp.GetString()!;
        }

        if (TryGetTargetUserIds(element, out var userIds))
        {
            foreach (var userId in userIds)
            {
                await SendToUserAsync(userId, clientEventName, payload);
            }

            return;
        }

        if (element.TryGetProperty("TargetUserId", out var targetProp)
            && targetProp.TryGetInt32(out var targetUserId))
        {
            await SendToUserAsync(targetUserId, clientEventName, payload);
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

    private async Task SendToUserAsync(int userId, string clientEventName, object payload)
    {
        _logger.LogDebug(
            "SignalR domain event {EventName} -> user {UserId}",
            clientEventName,
            userId);

        await _hubContext.Clients
            .Group(SystemHub.UserGroup(userId))
            .SendAsync(clientEventName, payload);
    }
}
