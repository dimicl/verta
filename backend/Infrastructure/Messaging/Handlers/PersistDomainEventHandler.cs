using System.Text.Json;
using backend.Application.Interfaces.Messaging;

namespace backend.Infrastructure.Messaging.Handlers;

public class PersistDomainEventHandler : IIntegrationEventHandler
{
    private readonly IDomainEventLogRepository _repo;
    private readonly ILogger<PersistDomainEventHandler> _logger;

    public PersistDomainEventHandler(
        IDomainEventLogRepository repo,
        ILogger<PersistDomainEventHandler> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    public string QueueName => MessagingTopology.DomainEventsQueue;
    public string BindingRoutingKey => "domain.#";

    public async Task HandleAsync(IntegrationEventEnvelope envelope, CancellationToken cancellationToken)
    {
        var payloadJson = envelope.Payload is string s
            ? s
            : JsonSerializer.Serialize(envelope.Payload);

        await _repo.Add(new DomainEventLog
        {
            EventName = envelope.EventName,
            Payload = payloadJson,
            QueueName = QueueName,
            ReceivedAt = DateTime.UtcNow
        });

        _logger.LogInformation("Persisted domain event {EventName}", envelope.EventName);
    }
}
