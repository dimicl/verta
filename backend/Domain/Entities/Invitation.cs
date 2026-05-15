public class Invitation
{
    public int Id { get; set; }

    public int WorkspaceId { get; set; }
    public Workspace? Workspace { get; set; }

    public int UserId { get; set; }
    public User? User { get; set; }

    public UserRole Role { get; set; }

    public bool IsAccepted { get; set; } = true;
}