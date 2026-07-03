using Microsoft.AspNetCore.Http;

namespace backend.Application.Interfaces;

public interface IFileStorageService
{
    Task<(string FileUrl, string? ThumbnailUrl)> SaveWorkItemFileAsync(
        int workItemId,
        IFormFile file);

    Task DeleteByUrlsAsync(string fileUrl, string? thumbnailUrl);
}
