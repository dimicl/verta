using backend.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Authorize]
[Route("api/board-locks")]
public class BoardLockController : ControllerBase
{
    private readonly IBoardLockService _boardLockService;

    public BoardLockController(IBoardLockService boardLockService)
    {
        _boardLockService = boardLockService;
    }

    [HttpPost("open/{boardId:int}")]
    public async Task<IActionResult> OpenBoard(int boardId)
    {
        try
        {
            var result = await _boardLockService.OpenBoard(boardId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("close/{boardId:int}")]
    public async Task<IActionResult> CloseBoard(int boardId)
    {
        try
        {
            var result = await _boardLockService.CloseBoard(boardId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}