namespace backend.Application.Interfaces.Messaging;

/// <summary>
/// Standard envelope for all RabbitMQ messages.
/// New services should consume this shape and switch on EventName / RoutingKey.
/// </summary>
public sealed class IntegrationEventEnvelope
{
    public required string EventName { get; init; }
    public required string RoutingKey { get; init; }
    public required object Payload { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public string? CorrelationId { get; init; }
}

/// <summary>
/// Topology constants. Add new queues/bindings here when introducing a new consumer service.
/// </summary>
public static class MessagingTopology
{
    public const string EventsExchange = "verta.events";

    public const string DomainEventsQueue = "domain-events";
    public const string UserEventsQueue = "user-events";

    /// <summary>Future example: notifications service queue.</summary>
    public const string NotificationsQueue = "notifications";

    public static string Domain(string eventName) => $"domain.{eventName}";
    public static string User(string eventName) => $"user.{eventName}";
}

public interface IMessageBus
{
    /// <summary>
    /// Publishes to the topic exchange with the given routing key.
    /// Bound queues (domain-events, user-events, …) receive matching messages.
    /// </summary>
    Task PublishAsync(
        string routingKey,
        string eventName,
        object payload,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Implement this in a new service/handler to react to RabbitMQ events.
/// Register as <c>services.AddSingleton&lt;IIntegrationEventHandler, YourHandler&gt;()</c>
/// (or scoped — the consumer creates a scope per message).
/// </summary>
public interface IIntegrationEventHandler
{
    /// <summary>Queue this handler listens on (declared + bound automatically).</summary>
    string QueueName { get; }

    /// <summary>
    /// Binding key on the topic exchange, e.g. <c>domain.#</c> or <c>domain.WorkItemAssigned</c>.
    /// </summary>
    string BindingRoutingKey { get; }

    /// <summary>
    /// Optional filter after dequeue. Return false to ignore (still ack).
    /// Default: accept all messages on the queue.
    /// </summary>
    bool CanHandle(IntegrationEventEnvelope envelope) => true;

    Task HandleAsync(IntegrationEventEnvelope envelope, CancellationToken cancellationToken);
}
