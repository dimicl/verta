namespace backend.Application.Interfaces;

public interface IWorkspaceService
{
    Task<WorkspaceResponse> Create(WorkspaceRequest request);

    Task<WorkspaceResponse> GetByOwnerId();
}