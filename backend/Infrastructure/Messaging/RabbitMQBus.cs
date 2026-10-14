using System.Text;
using System.Text.Json;
using backend.Application.Interfaces;
using backend.Application.Interfaces.Messaging;
using RabbitMQ.Client;

namespace backend.Infrastructure.Messaging;

public class RabbitMQBus : IMessageBus
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IRabbitMqConnectionManager _connectionManager;

    public RabbitMQBus(IRabbitMqConnectionManager connectionManager)
    {
        _connectionManager = connectionManager;
    }

    public async Task PublishAsync(
        string routingKey,
        string eventName,
        object payload,
        CancellationToken cancellationToken = default)
    {
        var connection = await _connectionManager.GetConnectionAsync(cancellationToken);

        await using var channel = await connection.CreateChannelAsync(
            cancellationToken: cancellationToken);

        await channel.ExchangeDeclareAsync(
            exchange: MessagingTopology.EventsExchange,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);

        // Ensure default audit queues exist and are bound (safe if already declared).
        await EnsureQueueBoundAsync(
            channel,
            MessagingTopology.DomainEventsQueue,
            "domain.#",
            cancellationToken);
        await EnsureQueueBoundAsync(
            channel,
            MessagingTopology.UserEventsQueue,
            "user.#",
            cancellationToken);

        var envelope = new IntegrationEventEnvelope
        {
            EventName = eventName,
            RoutingKey = routingKey,
            Payload = payload,
            CreatedAt = DateTime.UtcNow,
            CorrelationId = Guid.NewGuid().ToString("N")
        };

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(envelope, JsonOptions));

        var properties = new BasicProperties
        {
            Persistent = true,
            ContentType = "application/json",
            Type = eventName,
            CorrelationId = envelope.CorrelationId
        };

        await channel.BasicPublishAsync(
            exchange: MessagingTopology.EventsExchange,
            routingKey: routingKey,
            mandatory: false,
            basicProperties: properties,
            body: body,
            cancellationToken: cancellationToken);
    }

    private static async Task EnsureQueueBoundAsync(
        IChannel channel,
        string queueName,
        string bindingKey,
        CancellationToken cancellationToken)
    {
        await channel.QueueDeclareAsync(
            queue: queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);

        await channel.QueueBindAsync(
            queue: queueName,
            exchange: MessagingTopology.EventsExchange,
            routingKey: bindingKey,
            arguments: null,
            cancellationToken: cancellationToken);
    }
}
