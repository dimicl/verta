namespace backend.Application.Interfaces;

public interface IMessageBus
{
    Task PublishAsync(string queueName, string message, CancellationToken cancellationToken = default);
}
