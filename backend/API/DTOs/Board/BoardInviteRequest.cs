public record BoardInviteRequest
{
    public required int BoardId { get; set; }
    public required string Email { get; set; }
}
