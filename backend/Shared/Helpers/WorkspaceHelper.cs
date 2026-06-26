public static class WorkspaceHelper
{
    public static WorkspaceResponse ToResponse(Workspace workspace)
    {
        return new WorkspaceResponse
        {
            Id = workspace.Id,
            Name = workspace.Name,
            OwnerId = workspace.OwnerId,
            CreatedAt = workspace.CreatedAt
        };
    }
}