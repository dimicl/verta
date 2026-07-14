using backend.Application.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

using backend.Application.Exceptions;
namespace backend.Infrastructure.Storage;

public class LocalFileStorageService : IFileStorageService
{
    private const long MaxFileSizeBytes = 100 * 1024 * 1024;

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        "pdf", "png", "jpg", "jpeg", "gif", "avi", "mov", "mp4",
    };

    private readonly string _uploadRoot;

    public LocalFileStorageService(IWebHostEnvironment environment)
    {
        _uploadRoot = Path.Combine(environment.ContentRootPath, "uploads");
        Directory.CreateDirectory(_uploadRoot);
    }

    public async Task<(string FileUrl, string? ThumbnailUrl)> SaveWorkItemFileAsync(
        int workItemId,
        IFormFile file)
    {
        if (file == null || file.Length == 0)
            throw new ValidationException("File is required.");

        if (file.Length > MaxFileSizeBytes)
            throw new ValidationException("File size exceeds the 100 MB limit.");

        var extension = Path.GetExtension(file.FileName).TrimStart('.').ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension))
            throw new ValidationException("Unsupported file type.");

        var workItemFolder = Path.Combine(_uploadRoot, "work-items", workItemId.ToString());
        Directory.CreateDirectory(workItemFolder);

        var storedFileName = $"{Guid.NewGuid():N}_{SanitizeFileName(file.FileName)}";
        var absolutePath = Path.Combine(workItemFolder, storedFileName);

        await using (var stream = new FileStream(absolutePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var relativeUrl = $"/uploads/work-items/{workItemId}/{storedFileName}";
        string? thumbnailUrl = null;

        if (IsImageExtension(extension))
        {
            thumbnailUrl = relativeUrl;
        }

        return (relativeUrl, thumbnailUrl);
    }

    public Task DeleteByUrlsAsync(string fileUrl, string? thumbnailUrl)
    {
        DeleteIfExists(fileUrl);

        if (!string.IsNullOrWhiteSpace(thumbnailUrl) &&
            !string.Equals(thumbnailUrl, fileUrl, StringComparison.OrdinalIgnoreCase))
        {
            DeleteIfExists(thumbnailUrl);
        }

        return Task.CompletedTask;
    }

    private void DeleteIfExists(string relativeUrl)
    {
        if (string.IsNullOrWhiteSpace(relativeUrl))
            return;

        var normalized = relativeUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        if (normalized.StartsWith($"uploads{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized[("uploads".Length + 1)..];
        }

        var absolutePath = Path.Combine(_uploadRoot, normalized);

        if (File.Exists(absolutePath))
        {
            File.Delete(absolutePath);
        }
    }

    private static bool IsImageExtension(string extension) =>
        extension is "png" or "jpg" or "jpeg" or "gif";

    private static string SanitizeFileName(string fileName)
    {
        var name = Path.GetFileName(fileName);
        foreach (var invalidChar in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(invalidChar, '_');
        }

        return name;
    }
}
