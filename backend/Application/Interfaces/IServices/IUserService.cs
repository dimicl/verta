public interface IUserService
{
    Task<UserResponse> GetById(int id);
    Task<UserResponse> GetByEmail(string email);
    Task<UserResponse> GetCurrentUser();    
}