public class DomainEventLog
{
    public int Id { get; set; }
    public required string EventName { get; set; }
    public required string Payload { get; set; }
    public required string QueueName { get; set; }
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
}