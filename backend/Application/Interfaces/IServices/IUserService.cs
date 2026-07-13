public interface IUserService
{
    Task<UserResponse> GetById(int id);
    Task<UserResponse> GetCurrentUser();    
}