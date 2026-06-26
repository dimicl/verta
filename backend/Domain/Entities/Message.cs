public class Message
{
    public int Id { get; set; }
    public required string Content { get; set; }
    public int SenderId { get; set; }
    public int ConversationId { get; set; }
    public bool IsEdited { get; set; } = false;
    public bool IsDeleted { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EditedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
}