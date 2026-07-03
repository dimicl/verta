using backend.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Authorize]
[Route("api/boards")]
public class BoardController : ControllerBase
{
    private readonly IBoardService _boardService;

    public BoardController(IBoardService boardService)
    {
        _boardService = boardService;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] BoardRequest request)
    {
        try
        {
            var result = await _boardService.Create(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{boardId:int}")]
    public async Task<IActionResult> GetById(int boardId)
    {
        try
        {
            var result = await _boardService.GetById(boardId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("workspace/{workspaceId:int}")]
    public async Task<IActionResult> GetByWorkspaceId(int workspaceId)
    {
        try
        {
            var result = await _boardService.GetByWorkspaceId(workspaceId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("invite")]
    public async Task<IActionResult> InviteToBoard([FromBody] BoardInviteRequest request)
    {
        try
        {
            await _boardService.InviteToBoard(request);
            return Ok(new { message = "User invited to board." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{boardId:int}/members")]
    public async Task<IActionResult> GetMembers(int boardId)
    {
        try
        {
            var result = await _boardService.GetMembersByBoardId(boardId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}