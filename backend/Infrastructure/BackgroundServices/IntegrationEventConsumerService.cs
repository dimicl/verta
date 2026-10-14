using System.Text;
using System.Text.Json;
using backend.Application.Interfaces.Messaging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace backend.Infrastructure.BackgroundServices;

/// <summary>
/// Single consumer host: discovers all <see cref="IIntegrationEventHandler"/> registrations,
/// declares their queues/bindings on <see cref="MessagingTopology.EventsExchange"/>,
/// and dispatches messages. Add a new service by implementing the handler interface + DI.
/// </summary>
public class IntegrationEventConsumerService : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<IntegrationEventConsumerService> _logger;

    public IntegrationEventConsumerService(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<IntegrationEventConsumerService> logger)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = CreateConnectionFactory();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var connection = await factory.CreateConnectionAsync(stoppingToken);
                await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

                await channel.ExchangeDeclareAsync(
                    exchange: MessagingTopology.EventsExchange,
                    type: ExchangeType.Topic,
                    durable: true,
                    autoDelete: false,
                    arguments: null,
                    cancellationToken: stoppingToken);

                await using var setupScope = _scopeFactory.CreateAsyncScope();
                var handlers = setupScope.ServiceProvider
                    .GetServices<IIntegrationEventHandler>()
                    .ToList();

                if (handlers.Count == 0)
                {
                    _logger.LogWarning("No IIntegrationEventHandler registered.");
                    await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
                    continue;
                }

                var queueBindings = handlers
                    .GroupBy(h => h.QueueName, StringComparer.Ordinal)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(h => h.BindingRoutingKey).Distinct(StringComparer.Ordinal).ToList(),
                        StringComparer.Ordinal);

                foreach (var (queueName, bindingKeys) in queueBindings)
                {
                    await channel.QueueDeclareAsync(
                        queue: queueName,
                        durable: true,
                        exclusive: false,
                        autoDelete: false,
                        arguments: null,
                        cancellationToken: stoppingToken);

                    foreach (var bindingKey in bindingKeys)
                    {
                        await channel.QueueBindAsync(
                            queue: queueName,
                            exchange: MessagingTopology.EventsExchange,
                            routingKey: bindingKey,
                            arguments: null,
                            cancellationToken: stoppingToken);
                    }

                    await channel.BasicQosAsync(0, 1, false, stoppingToken);

                    var consumer = new AsyncEventingBasicConsumer(channel);
                    var capturedQueue = queueName;

                    consumer.ReceivedAsync += async (_, ea) =>
                    {
                        try
                        {
                            var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                            var envelope = DeserializeEnvelope(json, ea.RoutingKey);
                            await DispatchAsync(capturedQueue, envelope, stoppingToken);
                            await channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to handle message on {Queue}. Requeueing.", capturedQueue);
                            await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true);
                        }
                    };

                    await channel.BasicConsumeAsync(
                        queue: queueName,
                        autoAck: false,
                        consumer: consumer,
                        cancellationToken: stoppingToken);

                    _logger.LogInformation(
                        "Consuming queue {Queue} with bindings [{Bindings}]",
                        queueName,
                        string.Join(", ", bindingKeys));
                }

                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RabbitMQ connection lost. Retry in 5s...");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task DispatchAsync(
        string queueName,
        IntegrationEventEnvelope envelope,
        CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var handlers = scope.ServiceProvider
            .GetServices<IIntegrationEventHandler>()
            .Where(h => string.Equals(h.QueueName, queueName, StringComparison.Ordinal))
            .Where(h => h.CanHandle(envelope))
            .ToList();

        foreach (var handler in handlers)
        {
            await handler.HandleAsync(envelope, cancellationToken);
        }
    }

    private static IntegrationEventEnvelope DeserializeEnvelope(string json, string routingKey)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // New envelope shape
        if (root.TryGetProperty("eventName", out _) || root.TryGetProperty("EventName", out _))
        {
            var eventName =
                root.TryGetProperty("eventName", out var enCamel) ? enCamel.GetString()
                : root.TryGetProperty("EventName", out var en) ? en.GetString()
                : "Unknown";

            var payloadElement =
                root.TryGetProperty("payload", out var pCamel) ? pCamel
                : root.TryGetProperty("Payload", out var p) ? p
                : root;

            var rk =
                root.TryGetProperty("routingKey", out var rkCamel) ? rkCamel.GetString()
                : root.TryGetProperty("RoutingKey", out var rkProp) ? rkProp.GetString()
                : routingKey;

            DateTime createdAt = DateTime.UtcNow;
            if (root.TryGetProperty("createdAt", out var caCamel) && caCamel.TryGetDateTime(out var ca1))
                createdAt = ca1;
            else if (root.TryGetProperty("CreatedAt", out var ca) && ca.TryGetDateTime(out var ca2))
                createdAt = ca2;

            string? correlationId = null;
            if (root.TryGetProperty("correlationId", out var cCamel))
                correlationId = cCamel.GetString();
            else if (root.TryGetProperty("CorrelationId", out var c))
                correlationId = c.GetString();

            return new IntegrationEventEnvelope
            {
                EventName = eventName ?? "Unknown",
                RoutingKey = rk ?? routingKey,
                Payload = JsonSerializer.Deserialize<object>(payloadElement.GetRawText(), JsonOptions)
                           ?? payloadElement.GetRawText(),
                CreatedAt = createdAt,
                CorrelationId = correlationId
            };
        }

        return new IntegrationEventEnvelope
        {
            EventName = "Unknown",
            RoutingKey = routingKey,
            Payload = json,
            CreatedAt = DateTime.UtcNow
        };
    }

    private ConnectionFactory CreateConnectionFactory() => new()
    {
        HostName = _configuration["RabbitMQ:Host"]
            ?? throw new InvalidOperationException("RabbitMQ:Host missing"),
        Port = int.Parse(_configuration["RabbitMQ:Port"] ?? "5672"),
        UserName = _configuration["RabbitMQ:Username"]
            ?? throw new InvalidOperationException("RabbitMQ:Username missing"),
        Password = _configuration["RabbitMQ:Password"]
            ?? throw new InvalidOperationException("RabbitMQ:Password missing")
    };
}
