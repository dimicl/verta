public class File
{
    public Guid Id { get; set; }
    public required string FileName { get; set; }
    public required string FileType { get; set; }
    public long FileSize { get; set; }
    public required string FileUrl { get; set; }
    public required string FileThumbnailUrl { get; set; }
   
}