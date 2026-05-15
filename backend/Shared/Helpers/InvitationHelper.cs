public static class InvitationHelper
{
    public static InvitationResponse ToResponse(Invitation invitation)
    {
        return new InvitationResponse
        {
            Id = invitation.Id,
            WorkspaceId = invitation.WorkspaceId,
            UserId = invitation.UserId,
            Role = invitation.Role,
            IsAccepted = invitation.IsAccepted
        };
    }
}