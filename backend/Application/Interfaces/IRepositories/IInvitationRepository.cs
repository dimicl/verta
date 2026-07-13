public interface IInvitationRepository : IGenericRepository<Invitation>
{
    Task<Invitation?> GetByWorkspaceAndEmailAsync(int workspaceId, string email);
}