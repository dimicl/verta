using System.Text.Json;
using backend.Application.Interfaces;

public class RabbitMqDomainEventObserver : IDomainEventObserver
{
    private readonly IMessageBus _messageBus;

    public RabbitMqDomainEventObserver(IMessageBus messageBus)
    {
        _messageBus = messageBus;
    }

    public async Task UpdateAsync(string eventName, object payload)
    {
        var message = JsonSerializer.Serialize(new
        {
            EventName = eventName,
            Payload = payload,
            CreatedAt = DateTime.UtcNow
        });

        await _messageBus.PublishAsync("domain-events", message);
    }
}