using Microsoft.AspNetCore.Mvc;
using backend.Application.Interfaces;

namespace backend.API.Controllers;

[ApiController]
[Route("api")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        await _authService.Register(request);
        return Ok(new { message = "User created successfully" });
    }
    
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var response = await _authService.Login(request);
        return Ok(response);
    }
}