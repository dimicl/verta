namespace backend.Application.Interfaces;

using Microsoft.AspNetCore.Http;

public interface IWorkItemFileService
{
    Task<WorkItemFileResponse> Upload(int workItemId, IFormFile file, int? subWorkItemId = null);
    Task<WorkItemFileResponse> Create(WorkItemFileRequest request);
    Task<List<WorkItemFileResponse>> GetByWorkItemId(int workItemId);
    Task<List<WorkItemFileResponse>> GetBySubWorkItemId(int subWorkItemId);
    Task Delete(int fileId);
}