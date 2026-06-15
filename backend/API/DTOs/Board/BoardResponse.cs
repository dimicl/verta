public record BoardResponse
{
    public required int Id { get; set; }
    public required string Name { get; set; }
    public required int WorkspaceId { get; set; }
    public required int OwnerId { get; set; }
    public required DateTime CreatedAt { get; set; }
}