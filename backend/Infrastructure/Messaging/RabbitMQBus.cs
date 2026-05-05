using System.Text;
using backend.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;

namespace backend.Infrastructure.Messaging;

public class RabbitMQBus : IMessageBus
{
    private readonly IConfiguration _configuration;

    public RabbitMQBus(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task PublishAsync(string queueName, string message, CancellationToken cancellationToken = default)
    {
        var host = _configuration["RabbitMQ:Host"] ?? throw new InvalidOperationException("RabbitMQ host is not configured");
        var port = _configuration["RabbitMQ:Port"] ?? throw new InvalidOperationException("RabbitMQ port is not configured");
        var username = _configuration["RabbitMQ:Username"] ?? throw new InvalidOperationException("RabbitMQ username is not configured");
        var password = _configuration["RabbitMQ:Password"] ?? throw new InvalidOperationException("RabbitMQ password is not configured");

        var factory = new ConnectionFactory
        {
            HostName = host,
            Port = int.Parse(port),
            UserName = username,
            Password = password
        };

        await using var connection = await factory.CreateConnectionAsync(cancellationToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(
            queue: queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);

        var body = Encoding.UTF8.GetBytes(message);
        await channel.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: queueName,
            mandatory: false,
            body: body,
            cancellationToken: cancellationToken);
    }
}
