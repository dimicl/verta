using backend.Application.Interfaces;
using RabbitMQ.Client;

namespace backend.Infrastructure.Messaging;

public sealed class RabbitMqConnectionManager : IRabbitMqConnectionManager
{
    private readonly ConnectionFactory _factory;
    private readonly ILogger<RabbitMqConnectionManager> _logger;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private IConnection? _connection;

    public RabbitMqConnectionManager(
        IConfiguration configuration,
        ILogger<RabbitMqConnectionManager> logger)
    {
        _logger = logger;
        _factory = new ConnectionFactory
        {
            HostName = configuration["RabbitMQ:Host"]
                ?? throw new InvalidOperationException("RabbitMQ:Host missing"),
            Port = int.Parse(configuration["RabbitMQ:Port"] ?? "5672"),
            UserName = configuration["RabbitMQ:Username"]
                ?? throw new InvalidOperationException("RabbitMQ:Username missing"),
            Password = configuration["RabbitMQ:Password"]
                ?? throw new InvalidOperationException("RabbitMQ:Password missing")
        };
    }

    public async Task<IConnection> GetConnectionAsync(CancellationToken ct = default)
    {
        if (_connection is { IsOpen: true })
            return _connection;

        await _lock.WaitAsync(ct);
        try
        {
            if (_connection is { IsOpen: true })
                return _connection;

            if (_connection != null)
            {
                await _connection.DisposeAsync();
                _connection = null;
            }

            _logger.LogInformation("Opening RabbitMQ connection...");
            _connection = await _factory.CreateConnectionAsync(ct);
            _logger.LogInformation("RabbitMQ connection established.");
            return _connection;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection != null)
        {
            await _connection.DisposeAsync();
            _connection = null;
        }
        _lock.Dispose();
    }
}