public record WorkItemResponse
{
    public required int Id { get; set; }

    public required string Name { get; set; }
    public required string Description { get; set; }

    public required WorkItemStatus Status { get; set; }
    public required Priority Priority { get; set; }

    public required int BoardId { get; set; }
    public int? SprintId { get; set; }

    public required int CreatedByUserId { get; set; }
    public int? AssignedUserId { get; set; }

    public required DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}