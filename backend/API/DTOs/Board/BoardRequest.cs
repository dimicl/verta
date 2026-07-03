public record BoardRequest
{
    public required string Name { get; set; }
    public required int WorkspaceId { get; set; }
    public List<string>? InviteEmails { get; set; }
}
