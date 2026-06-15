namespace backend.Application.Interfaces;

public interface IWorkItemFileService
{
    Task<WorkItemFileResponse> Create(WorkItemFileRequest request);
    Task<List<WorkItemFileResponse>> GetByWorkItemId(int workItemId);
    Task Delete(int fileId);
}