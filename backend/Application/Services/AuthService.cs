using backend.Application.Interfaces;
using backend.Infrastructure.Persistence;
using backend.Shared.Helpers;

namespace backend.Application.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IUserRepository _userRepository;
    public AuthService(AppDbContext context, IConfiguration configuration, IUserRepository userRepository)
    {
        _context = context;
        _configuration = configuration;
        _userRepository = userRepository;
    }

    public async Task<AuthResponse> Register(RegisterRequest request)
    {
        var passwordHash = PasswordHasher.Hash(request.Password);
        var user = new User
        {
            Email = request.Email,
            Password = passwordHash,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Role = UserRole.Member,
            Status = "active",
            CreatedAt = DateTime.UtcNow.ToString(),
            UpdatedAt = DateTime.UtcNow.ToString()
        };
        
        var createdUser = await _userRepository.Create(user);
        return AuthHelper.BuildAuthResponse(createdUser, _configuration);
        

    }
    public async Task<AuthResponse> Login(LoginRequest request)
    {
        var user = await _userRepository.GetByEmail(request.Email);
        if (user == null)
            throw new Exception("User not found");
        
        if (!PasswordHasher.Verify(request.Password, user.Password))
            throw new Exception("Invalid password");

        return AuthHelper.BuildAuthResponse(user, _configuration);
    }
}