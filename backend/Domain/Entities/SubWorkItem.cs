public class SubWorkItem
{
    public int Id { get; set; }

    public required string Name { get; set; }
    public required string Description { get; set; }

    public WorkItemStatus Status { get; set; } = WorkItemStatus.ToDo;

    public int WorkItemId { get; set; }
    public WorkItem? WorkItem { get; set; }

    public int UserId { get; set; }
    public User? User { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}