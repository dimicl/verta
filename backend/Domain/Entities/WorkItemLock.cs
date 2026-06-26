public class WorkItemLock
{
    public int Id { get; set; }
    public int WorkItemId { get; set; }
    public WorkItem? WorkItem { get; set; }
    public int LockedByUserId { get; set; }
    public User? LockedByUser { get; set; }
    public DateTime LockedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
}