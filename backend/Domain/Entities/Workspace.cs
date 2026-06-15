public class Workspace
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public int OwnerId { get; set; }

    public User? Owner { get; set; }

    public List<WorkspaceMember>? Members { get; set; } 

    public List<Board>? Boards { get; set; }

    public DateTime CreatedAt { get; set; }
}