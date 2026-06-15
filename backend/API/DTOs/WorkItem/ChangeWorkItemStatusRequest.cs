public record ChangeWorkItemStatusRequest
{
    public required WorkItemStatus Status { get; set; }
}