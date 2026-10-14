using System.Text.Json;
using backend.Application.Interfaces.Messaging;

namespace backend.Infrastructure.Messaging.Handlers;

public class PersistUserEventHandler : IIntegrationEventHandler
{
    private readonly IDomainEventLogRepository _repo;
    private readonly ILogger<PersistUserEventHandler> _logger;

    public PersistUserEventHandler(
        IDomainEventLogRepository repo,
        ILogger<PersistUserEventHandler> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    public string QueueName => MessagingTopology.UserEventsQueue;
    public string BindingRoutingKey => "user.#";

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

        _logger.LogInformation("Persisted user event {EventName}", envelope.EventName);
    }
}
