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
   
   
}