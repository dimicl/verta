public class WorkItem
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public Status Status { get; set; }

    public string? Priority { get; set; }

    public int BoardId { get; set; }

    public int CreatedByUserId { get; set; }
    public int? AssignedUserId { get; set; }
    public List<File>? Files { get; set; }
    public List<Comment>? Comments { get; set; }

    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}