public record ConversationParticipantResponse
{
    public required int Id { get; set; }
    public required int UserId { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
}
