using backend.Infrastructure.Persistence;

public class WorkspaceMemberRepository: GenericRepository<WorkspaceMember>, IWorkspaceMemberRepository
{
    public WorkspaceMemberRepository(AppDbContext context): base(context){}

   
    
}