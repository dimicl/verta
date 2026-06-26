public record WorkItemLockResponse
{
    public required int WorkItemId { get; set; }
    public required int UserId { get; set; }
    public required string Mode { get; set; }
    public DateTime? LockedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
}