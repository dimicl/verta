public class WorkItemFile
{
    public int Id { get; set; }

    public int WorkItemId { get; set; }
    public WorkItem? WorkItem { get; set; }

    public int? SubWorkItemId { get; set; }
    public SubWorkItem? SubWorkItem { get; set; }

    public required string FileName { get; set; }
    public required string FileType { get; set; }
    public long FileSize { get; set; }
    public required string FileUrl { get; set; }
    public string? FileThumbnailUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}