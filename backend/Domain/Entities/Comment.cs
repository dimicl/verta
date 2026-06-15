public class Comment
{
    public int Id { get; set; }

    public required string Content { get; set; }

    public int WorkItemId { get; set; }
    public WorkItem? WorkItem { get; set; }

    public int UserId { get; set; }
    public User? User { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}