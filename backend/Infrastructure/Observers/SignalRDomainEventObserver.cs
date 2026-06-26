public class SignalRDomainEventObserver : IDomainEventObserver
{
    public Task UpdateAsync(string eventName, object payload)
    {
        return Task.CompletedTask;
    }
}