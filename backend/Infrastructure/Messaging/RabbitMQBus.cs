using System.Text;
using backend.Application.Interfaces;
using RabbitMQ.Client;

namespace backend.Infrastructure.Messaging;

public class RabbitMQBus : IMessageBus
{
    private readonly IRabbitMqConnectionManager _connectionManager;

    public RabbitMQBus(IRabbitMqConnectionManager connectionManager)
    {
        _connectionManager = connectionManager;
    }

    public async Task PublishAsync(
        string queueName,
        string message,
        CancellationToken cancellationToken = default)
    {
        var connection = await _connectionManager.GetConnectionAsync(cancellationToken);

        await using var channel = await connection.CreateChannelAsync(
            cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(
            queue: queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);

        var body = Encoding.UTF8.GetBytes(message);

        var properties = new BasicProperties
        {
            Persistent = true   
        };

        await channel.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: queueName,
            mandatory: false,
            basicProperties: properties,
            body: body,
            cancellationToken: cancellationToken);
    }
}