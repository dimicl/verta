public class Sprint
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public int BoardId { get; set; }
    public Board? Board { get; set; }

    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    public List<WorkItem>? WorkItems { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
