using backend.Application.Interfaces;
using backend.Shared.Helpers;

namespace backend.Application.Services;

public class AuthService : IAuthService
{
    private readonly IConfiguration _configuration;
    private readonly IUserRepository _userRepository;
    private readonly IMessageBus _messageBus;
    private readonly INotificationService _notificationService;

    public AuthService(
        IConfiguration configuration,
        IUserRepository userRepository,
        IMessageBus messageBus,
        INotificationService notificationService
        )
    {
        _configuration = configuration;
        _userRepository = userRepository;
        _messageBus = messageBus;
        _notificationService = notificationService;
    }

    public async Task Register(RegisterRequest request)
    {
        var existingUser = await _userRepository.GetByEmailAsync(request.Email);
        if (existingUser != null)
            throw new Exception("Email already exists");
        var passwordHash = PasswordHasher.Hash(request.Password);
        var user = new User
        {
            Email = request.Email,
            Password = passwordHash,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow,
            //UpdatedAt = DateTime.UtcNow
        };
        
        await _userRepository.Add(user);
        var updateMessage = $"User registered: {user.Email}";
        await _messageBus.PublishAsync("user-events", updateMessage);
        await _notificationService.SendUpdateAsync(updateMessage);
    }

    public async Task<AuthResponse> Login(LoginRequest request)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email);
        if (user == null)
            throw new Exception("User not found");
        
        if (!PasswordHasher.Verify(request.Password, user.Password))
            throw new Exception("Invalid password");

        if (user.Status != UserStatus.Active)
            throw new Exception("User account is not active");

        var updateMessage = $"User logged in: {user.Email}";
        await _messageBus.PublishAsync("user-events", updateMessage);
        await _notificationService.SendUpdateAsync(updateMessage);

        return AuthHelper.BuildAuthResponse(user, _configuration);
    }
}