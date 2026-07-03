public record SprintRequest
{
    public required string Name { get; set; }
    public required int BoardId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}
