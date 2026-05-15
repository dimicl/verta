public record InvitationRequest
{
    public required int UserId { get; set; }

    public required UserRole Role { get; set; }
}