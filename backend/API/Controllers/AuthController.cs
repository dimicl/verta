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
        //ovde treba li da se loguje kad se registruje ako da msm da treba da vraca id i token a ne samo komentar
    }
    
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var response = await _authService.Login(request);
        return Ok(response);
    }
}