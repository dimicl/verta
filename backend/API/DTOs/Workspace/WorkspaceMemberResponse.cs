public record WorkspaceMemberResponse
{
    public required int Id { get; set; }
    public required int WorkspaceId { get; set; }
    public required int OwnerId { get; set; }   
    public required UserRole Role { get; set; }
    public required DateTime? CreatedAt { get; set; }
}