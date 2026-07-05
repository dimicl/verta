public record WorkItemFileRequest
{
    public required int WorkItemId { get; set; }
    public int? SubWorkItemId { get; set; }

    public required string FileName { get; set; }
    public required string FileType { get; set; }
    public required long FileSize { get; set; }
    public required string FileUrl { get; set; }

    public string? FileThumbnailUrl { get; set; }
}