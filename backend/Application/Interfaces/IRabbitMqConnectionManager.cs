using RabbitMQ.Client;

namespace backend.Application.Interfaces;

public interface IRabbitMqConnectionManager : IAsyncDisposable
{
    Task<IConnection> GetConnectionAsync(CancellationToken ct = default);
}