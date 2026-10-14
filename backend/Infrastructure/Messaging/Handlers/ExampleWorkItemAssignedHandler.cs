using backend.Application.Interfaces.Messaging;

namespace backend.Infrastructure.Messaging.Handlers;

/// <summary>
/// Example stub for a future notifications service.
/// Uncomment DI registration when you implement real logic.
/// Binding: only WorkItemAssigned (not every domain event).
/// </summary>
public class ExampleWorkItemAssignedHandler : IIntegrationEventHandler
{
    private readonly ILogger<ExampleWorkItemAssignedHandler> _logger;

    public ExampleWorkItemAssignedHandler(ILogger<ExampleWorkItemAssignedHandler> logger)
    {
        _logger = logger;
    }

    public string QueueName => MessagingTopology.NotificationsQueue;
    public string BindingRoutingKey => MessagingTopology.Domain("WorkItemAssigned");

    public bool CanHandle(IntegrationEventEnvelope envelope) =>
        string.Equals(envelope.EventName, "WorkItemAssigned", StringComparison.Ordinal);

    public Task HandleAsync(IntegrationEventEnvelope envelope, CancellationToken cancellationToken)
    {
        // Future: send email / push notification.
        _logger.LogDebug(
            "Example handler received {EventName} (correlation {CorrelationId})",
            envelope.EventName,
            envelope.CorrelationId);
        return Task.CompletedTask;
    }
}
