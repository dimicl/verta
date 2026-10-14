using backend.Application.Interfaces;
using backend.Application.Interfaces.Messaging;
using backend.Shared.Helpers;
using backend.Application.Exceptions;

namespace backend.Application.Services;

public class AuthService : IAuthService
{
    private readonly IConfiguration _configuration;
    private readonly IUserRepository _userRepository;
    private readonly IMessageBus _messageBus;

    public AuthService(
        IConfiguration configuration,
        IUserRepository userRepository,
        IMessageBus messageBus)
    {
        _configuration = configuration;
        _userRepository = userRepository;
        _messageBus = messageBus;
    }

    public async Task<AuthResponse> Register(RegisterRequest request)
    {
        var existingUser = await _userRepository.GetByEmailAsync(request.Email);
        if (existingUser != null)
            throw new ValidationException("Email already exists");

        var passwordHash = PasswordHasher.Hash(request.Password);
        var user = new User
        {
            Email = request.Email,
            Password = passwordHash,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow,
        };

        await _userRepository.Add(user);

        try
        {
            await _messageBus.PublishAsync(
                MessagingTopology.User("UserRegistered"),
                "UserRegistered",
                new { UserId = user.Id, Email = user.Email });
        }
        catch (Exception)
        {
            // Do not fail registration if messaging is down.
        }

        return AuthHelper.BuildAuthResponse(user, _configuration);
    }

    public async Task<AuthResponse> Login(LoginRequest request)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email);
        if (user == null)
            throw new ValidationException("User not found");

        if (!PasswordHasher.Verify(request.Password, user.Password))
            throw new ValidationException("Invalid password");

        if (user.Status != UserStatus.Active)
            throw new ValidationException("User account is not active");

        await _messageBus.PublishAsync(
            MessagingTopology.User("UserLoggedIn"),
            "UserLoggedIn",
            new { UserId = user.Id });

        return AuthHelper.BuildAuthResponse(user, _configuration);
    }
}
