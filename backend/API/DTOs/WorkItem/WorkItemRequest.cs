public record WorkItemRequest
{
    public required string Name { get; set; }
    public required string Description { get; set; }

    public required int BoardId { get; set; }

    public int? AssignedUserId { get; set; }

    public Priority Priority { get; set; } = Priority.Medium;
}