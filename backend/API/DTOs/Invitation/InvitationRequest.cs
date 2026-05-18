public record InvitationRequest
{
    public required int WorkspaceId { get; set; }

    public required string Email { get; set; }
}