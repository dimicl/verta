public static class WorkItemFileHelper
{
    public static WorkItemFileResponse ToResponse(WorkItemFile file)
    {
        var contentUrl = $"/api/work-item-files/{file.Id}/content";

        return new WorkItemFileResponse
        {
            Id = file.Id,
            WorkItemId = file.WorkItemId,
            FileName = file.FileName,
            FileType = file.FileType,
            FileSize = file.FileSize,
            FileUrl = contentUrl,
            FileThumbnailUrl = string.IsNullOrWhiteSpace(file.FileThumbnailUrl) ? null : contentUrl,
            CreatedAt = file.CreatedAt
        };
    }
}
