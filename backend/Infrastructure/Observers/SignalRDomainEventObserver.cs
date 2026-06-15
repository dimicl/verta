using backend.Application.Interfaces;

public class SignalRDomainEventObserver : IDomainEventObserver
{
    private readonly INotificationService _notificationService;

    public SignalRDomainEventObserver(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    public async Task UpdateAsync(string eventName, object payload)
    {
        await _notificationService.SendUpdateAsync($"{eventName}: {payload}");
    }
}