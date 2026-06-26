public class Conversation
{
    public int Id { get; set; }
    public ConversationType Type { get; set; } = ConversationType.Direct;
    public string? Name { get; set; }
    public int CreatedByUserId { get; set; }
    public User? CreatedByUser { get; set; }
    public required List<ConversationParticipant> Participants { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}