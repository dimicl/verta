public record WorkspaceMemberRequest
{
    public required int WorkspaceId { get; set; }
    public required int OwnerId { get; set; }   
    public required DateTime CreatedAt { get; set; }
}