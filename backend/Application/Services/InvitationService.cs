using backend.Application.Interfaces;
using backend.Shared.Helpers;

namespace backend.Application.Services;

public class InvitationService : IInvitationService
{
    private readonly IInvitationRepository _invitationRepo;
    private readonly IWorkspaceRepository _workspaceRepo;
    private readonly IWorkspaceMemberRepository _workspaceMemberRepo;
    private readonly IUserContext _userContext;

    public InvitationService(
        IInvitationRepository invitationRepo,
        IWorkspaceRepository workspaceRepo,
        IWorkspaceMemberRepository workspaceMemberRepo,
        IUserContext userContext)
    {
        _invitationRepo = invitationRepo;
        _workspaceRepo = workspaceRepo;
        _workspaceMemberRepo = workspaceMemberRepo;
        _userContext = userContext;
    }

    public async Task<InvitationResponse> InviteUser(int workspaceId, InvitationRequest request)
    {
        if (request == null)
        {
            throw new Exception("Request not found.");
        }

        var currentUserId = _userContext.GetUserId();

        var workspace = await _workspaceRepo.GetById(workspaceId);

        if (workspace == null)
        {
            throw new Exception("Workspace does not exist.");
        }

        if (workspace.OwnerId != currentUserId)
        {
            throw new Exception("Only workspace owner can invite users.");
        }

        if (request.UserId == currentUserId)
        {
            throw new Exception("You cannot invite yourself.");
        }

        var existingMember = await _workspaceMemberRepo.GetByWorkspaceAndUserIdAsync(
            workspaceId,
            request.UserId
        );

        if (existingMember != null)
        {
            throw new Exception("User is already member of this workspace.");
        }

        var existingInvitation = await _invitationRepo.GetByWorkspaceAndUserIdAsync(
            workspaceId,
            request.UserId
        );

        if (existingInvitation != null)
        {
            throw new Exception("User is already invited to this workspace.");
        }

        var invitation = new Invitation
        {
            WorkspaceId = workspaceId,
            UserId = request.UserId,
            Role = request.Role,
            IsAccepted = true
        };

        var createdInvitation = await _invitationRepo.Add(invitation);

        var member = new WorkspaceMember
        {
            WorkspaceId = workspaceId,
            UserId = request.UserId,
            Role = request.Role,
            CreatedAt = DateTime.UtcNow
        };

        await _workspaceMemberRepo.Add(member);

        return InvitationHelper.ToResponse(createdInvitation);
    }
}