public class Workspace
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public int OwnerId { get; set; }

    public DateTime CreatedAt { get; set; }
}