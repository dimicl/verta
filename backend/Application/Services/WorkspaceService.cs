using System.Linq;
using backend.Application.Interfaces;
using backend.Shared.Helpers;
using Microsoft.EntityFrameworkCore;

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


   try{
    var workspaceResponse = await _repo.Add(workspace);

    var member = new WorkspaceMember
    {
        WorkspaceId = workspaceResponse.Id,
        UserId = userId,
        Role = UserRole.Owner,
        CreatedAt = DateTime.UtcNow
    };

    await _memberRepo.Add(member);
     
    return WorkspaceHelper.ToResponse(workspaceResponse);
   }
   catch(DbUpdateException)
   {
         throw new Exception("User already has a workspace.");
   }
   }

   public async Task<WorkspaceResponse> GetByOwnerId()
   {
      var userId = _userContext.GetUserId();

      var memberships = await _memberRepo.GetByUserIdAsync(userId);
      var invitedMembership = memberships.FirstOrDefault(member => member.Role != UserRole.Owner);

      if (invitedMembership != null)
      {
          var invitedWorkspace = await _repo.GetById(invitedMembership.WorkspaceId);
          if (invitedWorkspace != null)
          {
              return WorkspaceHelper.ToResponse(invitedWorkspace);
          }
      }

      var ownerWorkspace = await _repo.GetByOwnerIdAsync(userId);
      if (ownerWorkspace != null)
      {
         return WorkspaceHelper.ToResponse(ownerWorkspace);
      }

      throw new Exception("Workspace does not exist.");
   }

   public async Task<WorkspaceResponse> Update(int workspaceId, WorkspaceRequest request)
   {
      if (request == null)
         throw new Exception("Request not found.");

      if (string.IsNullOrWhiteSpace(request.Name))
         throw new Exception("Workspace name is required.");

      var userId = _userContext.GetUserId();
      var workspace = await _repo.GetById(workspaceId);

      if (workspace == null)
         throw new Exception("Workspace does not exist.");

      if (workspace.OwnerId != userId)
         throw new Exception("You are not the owner of this workspace.");

      workspace.Name = request.Name.Trim();
      await _repo.Update(workspace);

      return WorkspaceHelper.ToResponse(workspace);
   }

   public async Task Delete(int workspaceId)
   {
      var userId = _userContext.GetUserId();
      var workspace = await _repo.GetById(workspaceId);

      if (workspace == null)
         throw new Exception("Workspace does not exist.");

      if (workspace.OwnerId != userId)
         throw new Exception("You are not the owner of this workspace.");

      await _repo.Delete(workspace);
   }

}