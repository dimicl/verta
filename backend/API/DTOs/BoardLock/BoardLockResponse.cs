public record BoardLockResponse
{
    public required int BoardId { get; set; }
    public required int UserId { get; set; }
    public required string Mode { get; set; }
    public required DateTime? LockedAt { get; set; }
}