using backend.Application.Interfaces;
using backend.Infrastructure.Persistence;
using backend.Shared.Helpers;

namespace backend.Application.Services;

public class WorkspaceMemberService : IWorkspaceMemberService
{
    private readonly IWorkspaceMemberRepository _repo;

    public WorkspaceMemberService(IWorkspaceMemberRepository repo)
    {
        _repo = repo;
    }
   
   public async Task<WorkspaceMemberResponse> Create(WorkspaceMemberRequest request)
   {
        if(request == null)
        {
            throw new Exception("Request not found");
        }

        var entity = new WorkspaceMember {            
            UserId = request.OwnerId,
            WorkspaceId = request.WorkspaceId,
            Role = UserRole.Owner,
            CreatedAt = request.CreatedAt
        };

        var member = await _repo.Add(entity);


        return WorkspaceHelper.ToEntityMember(member);
   }


   

}