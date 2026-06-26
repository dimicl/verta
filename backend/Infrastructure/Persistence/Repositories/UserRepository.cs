using backend.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public class UserRepository: GenericRepository<User>, IUserRepository
{
    public UserRepository(AppDbContext context): base(context){}

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _dbSet.FirstOrDefaultAsync(u => u.Email == email);
    }
    
}