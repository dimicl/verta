namespace backend.Application.Interfaces;

public interface IBoardAccessService
{
    Task<WorkspaceMember> EnsureBoardAccessAsync(Board board);
    Task<bool> HasFullWorkspaceBoardAccessAsync(int workspaceId, int userId);
}
