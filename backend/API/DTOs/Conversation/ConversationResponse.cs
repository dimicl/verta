public record ConversationResponse
{
    public required int Id { get; set; }
    public required string Type { get; set; }
    public string? Name { get; set; }
    public required DateTime CreatedAt { get; set; }
    public required int UnreadCount { get; set; }    public required List<ConversationParticipantResponse> Participants { get; set; }}