using backend.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Authorize]
[Route("api/sprints")]
public class SprintController : ControllerBase
{
    private readonly ISprintService _sprintService;

    public SprintController(ISprintService sprintService)
    {
        _sprintService = sprintService;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SprintRequest request)
    {
        try
        {
            var result = await _sprintService.Create(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{sprintId:int}")]
    public async Task<IActionResult> GetById(int sprintId)
    {
        try
        {
            var result = await _sprintService.GetById(sprintId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("board/{boardId:int}")]
    public async Task<IActionResult> GetByBoardId(int boardId)
    {
        try
        {
            var result = await _sprintService.GetByBoardId(boardId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
