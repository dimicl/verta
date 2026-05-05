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

    [HttpGet("users/{id}")]
    public async Task<IActionResult> GetUserById(int id)
    {
        var user = await _userService.GetById(id);
        return Ok(user);
    }
}