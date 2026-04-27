public class Message
{
    public int Id { get; set; }
    public required string Content { get; set; }
    public int SenderId { get; set; }
    public int ConversationId { get; set; }
    public DateTime? CreatedAt { get; set; }
}