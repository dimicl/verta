public class Conversation
{
    public int Id { get; set; }
    public required List<ConversationParticipant> Participants {get; set; }
    public DateTime? CreatedAt { get; set; }
}