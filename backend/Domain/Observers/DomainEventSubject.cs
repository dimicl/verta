public class DomainEventSubject
{
    private readonly List<IDomainEventObserver> _observers;

    public DomainEventSubject(IEnumerable<IDomainEventObserver> observers)
    {
        _observers = observers.ToList();
    }

    public async Task NotifyAsync(string eventName, object payload)
    {
        foreach (var observer in _observers)
        {
            await observer.UpdateAsync(eventName, payload);
        }
    }
}