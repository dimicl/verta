public record CommentRequest
{
    public required int WorkItemId { get; set; }
    public int? SubWorkItemId { get; set; }
    public required string Content { get; set; }
}