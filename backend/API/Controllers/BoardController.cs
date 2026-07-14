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
        var result = await _boardService.Create(request);
        return Ok(result);
    }

    [HttpGet("{boardId:int}")]
    public async Task<IActionResult> GetById(int boardId)
    {
        var result = await _boardService.GetById(boardId);
        return Ok(result);
    }

    [HttpGet("workspace/{workspaceId:int}")]
    public async Task<IActionResult> GetByWorkspaceId(int workspaceId)
    {
        var result = await _boardService.GetByWorkspaceId(workspaceId);
        return Ok(result);
    }

    [HttpPost("invite")]
    public async Task<IActionResult> InviteToBoard([FromBody] BoardInviteRequest request)
    {
        await _boardService.InviteToBoard(request);
        return Ok(new { message = "User invited to board." });
    }

    [HttpGet("{boardId:int}/members")]
    public async Task<IActionResult> GetMembers(int boardId)
    {
        var result = await _boardService.GetMembersByBoardId(boardId);
        return Ok(result);
    }
}