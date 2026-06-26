using backend.Application.Interfaces;
using backend.Shared.Helpers;

namespace backend.Application.Services;

public class InvitationService : IInvitationService
{
    private readonly IInvitationRepository _invitationRepo;
    private readonly IWorkspaceRepository _workspaceRepo;
    private readonly IWorkspaceMemberRepository _workspaceMemberRepo;
    private readonly IUserContext _userContext;
    private readonly IUserRepository _userRepository;
    private readonly INotificationService _notificationService;

    public InvitationService(
        IInvitationRepository invitationRepo,
        IWorkspaceRepository workspaceRepo,
        IWorkspaceMemberRepository workspaceMemberRepo,
        IUserContext userContext,
        IUserRepository userRepository,
        INotificationService notificationService
    )
    {
        _invitationRepo = invitationRepo;
        _workspaceRepo = workspaceRepo;
        _workspaceMemberRepo = workspaceMemberRepo;
        _userContext = userContext;
        _userRepository = userRepository;
        _notificationService = notificationService;
    }

    public async Task<InvitationResponse> InviteUser(InvitationRequest request)
    {
        if (request == null)
        {
            throw new Exception("Request not found.");
        }

        var currentUserId = _userContext.GetUserId();

        var invitedUser = await _userRepository.GetByEmailAsync(request.Email);
        
        if(invitedUser == null)
        {
            throw new Exception("Invited user does not exist.");
        }

        var workspace = await _workspaceRepo.GetById(request.WorkspaceId);

        if (workspace == null)
        {
            throw new Exception("Workspace does not exist.");
        }

        if (workspace.OwnerId != currentUserId)
        {
            throw new Exception("Only workspace owner can invite users.");
        }

        if (invitedUser.Id == currentUserId)
        {
            throw new Exception("You cannot invite yourself.");
        }

        var existingMember = await _workspaceMemberRepo.GetByWorkspaceAndUserIdAsync(
            request.WorkspaceId,
            invitedUser.Id
        );

        if (existingMember != null)
        {
            throw new Exception("User is already member of this workspace.");
        }

        var existingInvitation = await _invitationRepo.GetByWorkspaceAndEmailAsync(
            request.WorkspaceId,
            request.Email
        );

        if (existingInvitation != null)
        {
            throw new Exception("User is already invited to this workspace.");
        }

        var invitation = new Invitation
        {
            WorkspaceId = workspace.Id,
            UserId = invitedUser.Id,
            Role = UserRole.Member,
            IsAccepted = true
        };

        var createdInvitation = await _invitationRepo.Add(invitation);

        var member = new WorkspaceMember
        {
            WorkspaceId = workspace.Id,
            UserId = invitedUser.Id,
            Role = UserRole.Member,
            CreatedAt = DateTime.UtcNow
        };

        await _workspaceMemberRepo.Add(member);

        await _notificationService.SendToUserAsync(
            invitedUser.Id,
            "WorkspaceInvitation",
            new
            {
                WorkspaceId = workspace.Id,
                WorkspaceName = workspace.Name,
                InvitedByUserId = currentUserId
            });

        return InvitationHelper.ToResponse(createdInvitation);
    }
}