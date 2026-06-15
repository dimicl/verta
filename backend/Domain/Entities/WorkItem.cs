public class WorkItem
{
    public int Id { get; set; }

    public required string Name { get; set; }
    public required string Description { get; set; }

    public WorkItemStatus Status { get; set; } = WorkItemStatus.ToDo;
    public Priority Priority { get; set; } = Priority.Medium;

    public int BoardId { get; set; }
    public Board? Board { get; set; }

    public int CreatedByUserId { get; set; }
    public User? CreatedByUser { get; set; }

    public int? AssignedUserId { get; set; }
    public User? AssignedUser { get; set; }

    public List<SubWorkItem>? SubWorkItems { get; set; }
    public List<WorkItemFile>? Files { get; set; }
    public List<Comment>? Comments { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}