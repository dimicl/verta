using backend.Application.Interfaces;
using backend.Infrastructure.Persistence;
using backend.Shared.Helpers;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace backend.Application.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IUserRepository _userRepository;

    public AuthService(
        AppDbContext context,
        IConfiguration configuration,
        IUserRepository userRepository      
        )
    {
        _context = context;
        _configuration = configuration;
        _userRepository = userRepository;
        
    }

    public async Task Register(RegisterRequest request)
    {
        var passwordHash = PasswordHasher.Hash(request.Password);
        var user = new User
        {
            Email = request.Email,
            Password = passwordHash,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Status = "active",
            CreatedAt = DateTime.UtcNow.ToShortDateString(),
            UpdatedAt = DateTime.UtcNow.ToShortDateString()
        };
        
        await _userRepository.Add(user);
    }

    public async Task<AuthResponse> Login(LoginRequest request)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email);
        if (user == null)
            throw new Exception("User not found");
        
        if (!PasswordHasher.Verify(request.Password, user.Password))
            throw new Exception("Invalid password");

        return AuthHelper.BuildAuthResponse(user, _configuration);
    }
}