namespace backend.Application.Interfaces;

public interface IInvitationService
{
    Task<InvitationResponse> InviteUser(int workspaceId, InvitationRequest request);
}