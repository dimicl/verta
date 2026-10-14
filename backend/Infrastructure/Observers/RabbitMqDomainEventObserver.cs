using backend.Application.Interfaces;
using backend.Application.Interfaces.Messaging;

public class RabbitMqDomainEventObserver : IDomainEventObserver
{
    private readonly IMessageBus _messageBus;

    public RabbitMqDomainEventObserver(IMessageBus messageBus)
    {
        _messageBus = messageBus;
    }

    public async Task UpdateAsync(string eventName, object payload)
    {
        await _messageBus.PublishAsync(
            MessagingTopology.Domain(eventName),
            eventName,
            payload);
    }
}
