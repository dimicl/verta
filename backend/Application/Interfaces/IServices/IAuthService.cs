namespace backend.Application.Interfaces;

public interface IAuthService
{
    Task Register(RegisterRequest request);
    Task<AuthResponse> Login(LoginRequest request);
}