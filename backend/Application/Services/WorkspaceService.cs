using backend.Application.Interfaces;
using backend.Infrastructure.Persistence;
using backend.Shared.Helpers;
using Microsoft.AspNetCore.Http.HttpResults;

namespace backend.Application.Services;

public class WorkspaceService : IWorkspaceService
{
    private readonly IWorkspaceRepository _repo;
    private readonly IWorkspaceMemberRepository _memberRepo;
    private readonly IUserContext _userContext;


    public WorkspaceService(IWorkspaceRepository repo, IWorkspaceMemberRepository memberRepo, IUserContext userContext)
    {
        _repo = repo;
        _memberRepo = memberRepo;
        _userContext = userContext;
    }
   
   public async Task<WorkspaceResponse> Create(WorkspaceRequest request)
   {
    if (request == null)
     {
        throw new Exception("Request not found");
     }

     var userId = _userContext.GetUserId();

     var existingWorkspace = await _repo.GetByOwnerIdAsync(userId);
     if(existingWorkspace != null)
        throw new Exception ("User already has a workspace.");
     

     var workspace = new Workspace {
        Name = request.Name,
        OwnerId = userId,
        CreatedAt = DateTime.UtcNow
     };

    var workspaceResponse = await _repo.Add(workspace);

    var member = new WorkspaceMember
    {
        WorkspaceId = workspaceResponse.Id,
        UserId = userId,
        Role = UserRole.Owner,
        CreatedAt = DateTime.UtcNow
    };

    await _memberRepo.Add(member);
     
     return WorkspaceHelper.ToEntity(workspaceResponse);

   }

   public async Task<WorkspaceResponse> GetByOwnerId()
   {
      var userId = _userContext.GetUserId();
      var workspace = await _repo.GetByOwnerIdAsync(userId);
      if(workspace != null)
      {
         return WorkspaceHelper.ToEntity(workspace);
      }
      throw new Exception("Workspace does not exist.");
   }

   

}