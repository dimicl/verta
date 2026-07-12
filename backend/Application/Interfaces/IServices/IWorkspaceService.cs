namespace backend.Application.Interfaces;

public interface IWorkspaceService
{
    Task<WorkspaceResponse> Create(WorkspaceRequest request);

    Task<WorkspaceResponse> GetByOwnerId();

    Task<WorkspaceResponse> Update(int workspaceId, WorkspaceRequest request);

    Task Delete(int workspaceId);
}