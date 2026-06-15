public record SubWorkItemRequest
{
    public required string Name { get; set; }
    public required string Description { get; set; }
    public required int WorkItemId { get; set; }
}