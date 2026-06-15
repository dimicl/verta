public record SubWorkItemResponse
{
    public required int Id { get; set; }

    public required string Name { get; set; }
    public required string Description { get; set; }

    public required WorkItemStatus Status { get; set; }

    public required int WorkItemId { get; set; }
    public required int UserId { get; set; }

    public required DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}