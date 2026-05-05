public record WorkspaceResponse
{
    public required int Id { get; set;}
    public required string Name { get; set; }
    public required int OwnerId { get; set; }   
    public required DateTime CreatedAt { get; set; }
}