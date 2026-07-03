public record SprintResponse
{
    public required int Id { get; set; }
    public required string Name { get; set; }
    public required int BoardId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public required DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
