public interface IDomainEventObserver
{
    Task UpdateAsync(string eventName, object payload);
}