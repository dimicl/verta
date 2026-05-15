public interface IInvitationRepository : IGenericRepository<Invitation>
{
    Task<Invitation?> GetByWorkspaceAndUserIdAsync(int workspaceId, int userId);

    Task<List<Invitation>> GetByWorkspaceIdAsync(int workspaceId);

    Task<List<Invitation>> GetByUserIdAsync(int userId);
}