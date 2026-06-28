using backend.Application.Interfaces;
using backend.Application.Exceptions;

namespace backend.Application.Services;

public class WorkspaceMemberService : IWorkspaceMemberService
{
    private readonly IWorkspaceMemberRepository _memberRepo;
    private readonly IWorkspaceRepository _workspaceRepo;
    private readonly IUserContext _userContext;

    public WorkspaceMemberService(
        IWorkspaceMemberRepository memberRepo,
        IWorkspaceRepository workspaceRepo,
        IUserContext userContext)
    {
        _memberRepo = memberRepo;
        _workspaceRepo = workspaceRepo;
        _userContext = userContext;
    }

    public async Task<List<WorkspaceMemberResponse>> GetMembersByWorkspaceId(int workspaceId)
    {
        var workspace = await _workspaceRepo.GetById(workspaceId);
        if (workspace == null)
            throw new NotFoundException("Workspace does not exist.");

        var currentUserId = _userContext.GetUserId();
        var currentMember = await _memberRepo.GetByWorkspaceAndUserIdAsync(workspaceId, currentUserId);
        if (currentMember == null)
            throw new ForbiddenException("You are not a member of this workspace.");

        var members = await _memberRepo.GetByWorkspaceIdAsync(workspaceId);
        return members.Select(m => new WorkspaceMemberResponse
        {
            Id = m.Id,
            WorkspaceId = m.WorkspaceId,
            UserId = m.UserId,
            Role = m.Role,
            CreatedAt = m.CreatedAt,
            FirstName = m.User?.FirstName ?? string.Empty,
            LastName = m.User?.LastName ?? string.Empty
        }).ToList();
    }
}