public record CommentResponse
{
    public required int Id { get; set; }
    public required string Content { get; set; }

    public required int WorkItemId { get; set; }
    public required int UserId { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }

    public required DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}