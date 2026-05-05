using backend.Infrastructure.Persistence;
using backend.Shared.Helpers;

namespace backend.Application.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _repo;

    public UserService(IUserRepository repo)
    {
        _repo = repo;
    }
   
   public async Task<UserResponse> GetById(int id)
   {
     var user = await _repo.GetById(id);
     if (user == null)
     {
        throw new Exception("User not found");
     }
     return UserHelper.ToEntity(user);

   }

   public async Task<UserResponse> GetByEmail(string email)
   {
     var user = await _repo.GetByEmailAsync(email);
     if (user == null)
     {
        throw new Exception("User not found");
     }
     return UserHelper.ToEntity(user);
   }

}