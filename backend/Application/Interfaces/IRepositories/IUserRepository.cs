public interface IUserRepository : IGenericRepository<User>
{
    Task<User?> GetByEmailAsync(string email);

}