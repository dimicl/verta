using backend.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace backend.Application.Services;

public class UserRepository: GenericRepository<User>, IUserRepository
{
    public UserRepository(AppDbContext context): base(context){}

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _dbSet.FirstOrDefaultAsync(u => u.Email == email);
    }
    
}