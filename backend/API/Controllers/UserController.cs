using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api")]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;
    public UserController(IUserService userService)
    {
        _userService = userService;
    }

    [Authorize]
    [HttpGet("users/me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        var user = await _userService.GetCurrentUser();
        return Ok(user);
    }

    [Authorize]
    [HttpGet("users/{id}")]
    public async Task<IActionResult> GetUserById(int id)
    {
        var user = await _userService.GetById(id);
        return Ok(user);
    }
}