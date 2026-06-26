public class ConversationParticipant
{
    public int Id { get; set; }
    public int ConversationId { get; set; }
    public Conversation? Conversation { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public bool IsMuted { get; set; }
    public bool IsArchived { get; set; }
    public int? LastReadMessageId { get; set; }
    public DateTime? LastReadAt { get; set; }
}