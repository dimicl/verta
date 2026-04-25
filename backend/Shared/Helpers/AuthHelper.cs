

using backend.Infrastructure.Persistence;

namespace backend.Shared.Helpers;

public static class AuthHelper
{
    public static async Task<User> CreateUser(  
        IUserRepository userRepository,
        string firstName,
        string lastName,
        string email,
        string password,
        UserRole role
    )
    {
        if (await userRepository.GetByEmail(email) != null)
            throw new Exception("Email already exists");
        

        var passwordHash = PasswordHasher.Hash(password);
        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            Password = passwordHash,
            Role = role,
        };
    
        await userRepository.Create(user);
        return user;
    }

    public static AuthResponse BuildAuthResponse(User user, IConfiguration config)
    {
        var token = JwtHelper.Generate(user, config);
        var expiresAt = DateTime.UtcNow.AddHours(1);

        return new AuthResponse
        {
            Token = token,
            ExpiresAt = expiresAt,
            User = new UserResponse
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Role = user.Role,
            }
        };
    }
}
