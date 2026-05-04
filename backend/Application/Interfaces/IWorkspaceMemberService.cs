namespace backend.Application.Interfaces;

public interface IWorkspaceMemberService
{
    Task<WorkspaceMemberResponse> Create(WorkspaceMemberRequest request);
}