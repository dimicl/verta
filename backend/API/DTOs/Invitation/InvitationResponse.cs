public record InvitationResponse
{
    public required int Id { get; set; }

    public required int WorkspaceId { get; set; }

    public required int UserId { get; set; }

    public required UserRole Role { get; set; }

    public required bool IsAccepted { get; set; }
}