public class WorkspaceMember
{
    public int Id { get; set; }
    public int WorkspaceId { get; set; }

    public Workspace? Workspace {get; set;}

    public User? User { get; set; }
    public int UserId { get; set; }
    public UserRole Role { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}