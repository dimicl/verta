public static class WorkspaceHelper
{
    public static WorkspaceResponse ToEntity(Workspace workspace)
    {
        return new WorkspaceResponse
        {
            Id = workspace.Id,
            Name = workspace.Name,
            OwnerId = workspace.OwnerId,
            CreatedAt = workspace.CreatedAt
        };
    }

    public static WorkspaceMemberResponse ToEntityMember(WorkspaceMember member)
    {
        return new WorkspaceMemberResponse
        {
            Id = member.Id,
            WorkspaceId = member.WorkspaceId,
            OwnerId = member.UserId,
            Role = member.Role,
            CreatedAt = member.CreatedAt
        };
    }

    public static WorkspaceMemberRequest FromResponseToRequest(WorkspaceResponse response)
    {
        return new WorkspaceMemberRequest
        {
            WorkspaceId = response.Id,
            OwnerId = response.OwnerId,
            CreatedAt = response.CreatedAt
        };
    }
}