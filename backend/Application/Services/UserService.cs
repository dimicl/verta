using backend.Shared.Helpers;

using backend.Application.Exceptions;
namespace backend.Application.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _repo;
    private readonly IUserContext _userContext;

    public UserService(IUserRepository repo, IUserContext userContext)
    {
        _repo = repo;
        _userContext = userContext;
    }
   
    public async Task<UserResponse> GetById(int id)
    {
        var user = await _repo.GetById(id);

        if (user == null)
            throw new NotFoundException("User not found");

        return UserHelper.ToResponse(user);
    }

    public async Task<UserResponse> GetCurrentUser()
    {
        var userId = _userContext.GetUserId();

        var user = await _repo.GetById(userId);

        if (user == null)
            throw new NotFoundException("User not found");

        return UserHelper.ToResponse(user);
    }
}