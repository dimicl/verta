public class BoardLock
{
    public int Id { get; set; }

    public int BoardId { get; set; }
    public Board? Board { get; set; }

    public int LockedByUserId { get; set; }
    public User? LockedByUser { get; set; }

    public DateTime LockedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
}