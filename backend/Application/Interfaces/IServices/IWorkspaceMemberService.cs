namespace backend.Application.Interfaces;

public interface IWorkspaceMemberService
{
    Task<List<WorkspaceMemberResponse>> GetMembersByWorkspaceId(int workspaceId);
}