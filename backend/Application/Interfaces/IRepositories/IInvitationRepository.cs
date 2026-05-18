public interface IInvitationRepository : IGenericRepository<Invitation>
{
    Task<Invitation?> GetByWorkspaceAndEmailAsync(int workspaceId, string email);

    Task<List<Invitation>> GetByWorkspaceIdAsync(int workspaceId);

    Task<List<Invitation>> GetByUserIdAsync(int userId);
}