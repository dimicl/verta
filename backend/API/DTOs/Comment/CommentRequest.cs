public record CommentRequest
{
    public required int WorkItemId { get; set; }
    public required string Content { get; set; }
}