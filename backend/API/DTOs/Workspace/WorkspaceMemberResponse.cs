public record WorkspaceMemberResponse
{
    public required int Id { get; set; }

    public required int WorkspaceId { get; set; }

    public required int UserId { get; set; }

    public required UserRole Role { get; set; }

    public required DateTime CreatedAt { get; set; }

    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public bool IsOnline { get; set; }
}