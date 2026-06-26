using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace backend.Infrastructure.BackgroundServices;

public class UserEventConsumerService : BackgroundService
{
    private const string QueueName = "user-events";
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<UserEventConsumerService> _logger;

    public UserEventConsumerService(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<UserEventConsumerService> logger)
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

                await channel.QueueDeclareAsync(
                    queue: QueueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null,
                    cancellationToken: stoppingToken);

                await channel.BasicQosAsync(
                    prefetchSize: 0,
                    prefetchCount: 1,
                    global: false,
                    cancellationToken: stoppingToken);

                var consumer = new AsyncEventingBasicConsumer(channel);
                consumer.ReceivedAsync += async (_, ea) =>
                {
                    try
                    {
                        var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                        await PersistEventAsync(json);
                        await channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
                        _logger.LogInformation("User event persisted.");
                    }
                    catch (System.Text.Json.JsonException ex)
                    {
                        _logger.LogWarning(ex, "Invalid JSON message, discarding.");
                        await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to persist user event. Requeueing.");
                        await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true);
                    }
                };

                await channel.BasicConsumeAsync(
                    queue: QueueName,
                    autoAck: false,
                    consumer: consumer,
                    cancellationToken: stoppingToken);

                _logger.LogInformation("UserEventConsumerService connected to RabbitMQ.");
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RabbitMQ connection lost in UserEventConsumerService. Retry in 5s...");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task PersistEventAsync(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var eventName = root.TryGetProperty("EventName", out var en)
            ? en.GetString() ?? "Unknown" : "Unknown";
        var payload = root.TryGetProperty("Payload", out var p)
            ? p.ToString() : json;

        await using var scope = _scopeFactory.CreateAsyncScope();
        var repo = scope.ServiceProvider.GetRequiredService<IDomainEventLogRepository>();
        await repo.Add(new DomainEventLog
        {
            EventName = eventName,
            Payload = payload,
            QueueName = QueueName,
            ReceivedAt = DateTime.UtcNow
        });
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