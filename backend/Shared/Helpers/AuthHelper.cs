namespace backend.Shared.Helpers;

public static class AuthHelper
{
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
                Status = UserStatus.Active
            }
        };
    }
}
