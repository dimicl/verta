public static class WorkItemFileHelper
{
    public static WorkItemFileResponse ToResponse(WorkItemFile file)
    {
        return new WorkItemFileResponse
        {
            Id = file.Id,
            WorkItemId = file.WorkItemId,
            FileName = file.FileName,
            FileType = file.FileType,
            FileSize = file.FileSize,
            FileUrl = file.FileUrl,
            FileThumbnailUrl = file.FileThumbnailUrl,
            CreatedAt = file.CreatedAt
        };
    }
}