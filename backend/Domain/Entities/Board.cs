public class Board
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public int WorkspaceId { get; set; }
    public Workspace? Workspace { get; set; }

    public int OwnerId { get; set; }
    public User? Owner { get; set; }

    public List<WorkItem>? WorkItems { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}