public record MessageResponse
{
    public required int Id { get; set; }
    public required int ConversationId { get; set; }
    public required int SenderId { get; set; }
    public required string Content { get; set; }
    public required bool IsEdited { get; set; }
    public required bool IsDeleted { get; set; }
    public required DateTime CreatedAt { get; set; }
    public DateTime? EditedAt { get; set; }
}