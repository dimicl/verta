namespace backend.Application.Interfaces;

using Microsoft.AspNetCore.Http;

public interface IWorkItemFileService
{
    Task<WorkItemFileResponse> Upload(int workItemId, IFormFile file);
    Task<WorkItemFileResponse> Create(WorkItemFileRequest request);
    Task<List<WorkItemFileResponse>> GetByWorkItemId(int workItemId);
    Task Delete(int fileId);
}