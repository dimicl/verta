namespace backend.Application.Interfaces;

public interface IInvitationService
{
    Task<InvitationResponse> InviteUser(InvitationRequest request);
}