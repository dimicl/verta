public class WorkItemLockInterest
{
    public int Id { get; set; }
    public int WorkItemId { get; set; }
    public WorkItem? WorkItem { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
}