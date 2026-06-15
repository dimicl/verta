public record ChangeSubWorkItemStatusRequest
{
    public required WorkItemStatus Status { get; set; }
}