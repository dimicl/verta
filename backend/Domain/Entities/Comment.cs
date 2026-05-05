public class Comment
{
    public int Id { get; set; }
    public required string Content { get; set; }
    public int TaskId { get; set; }
    public int UserId { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}