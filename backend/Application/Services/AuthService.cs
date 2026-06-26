using backend.Application.Interfaces;
using backend.Shared.Helpers;

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
        };

        await _userRepository.Add(user);

        // Publish ne blokira registraciju — greška se samo loguje
        try
        {
            await _messageBus.PublishAsync("user-events",
                System.Text.Json.JsonSerializer.Serialize(new
                {
                    EventName = "UserRegistered",
                    Payload = new { UserId = user.Id, Email = user.Email },
                    CreatedAt = DateTime.UtcNow
                }));
        }
        catch (Exception ex)
        {
            // Log only — ne propagiramo, korisnik je kreiran uspešno
            // U produkciji: ILogger<AuthService> ovde
            _ = ex;
        }

        return AuthHelper.BuildAuthResponse(user, _configuration);
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

        await _messageBus.PublishAsync("user-events",
            System.Text.Json.JsonSerializer.Serialize(new
            {
                EventName = "UserLoggedIn",
                Payload = new { UserId = user.Id },
                CreatedAt = DateTime.UtcNow
            }));

        return AuthHelper.BuildAuthResponse(user, _configuration);
    }
}