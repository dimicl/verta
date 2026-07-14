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
        var result = await _sprintService.Create(request);
        return Ok(result);
    }

    [HttpGet("{sprintId:int}")]
    public async Task<IActionResult> GetById(int sprintId)
    {
        var result = await _sprintService.GetById(sprintId);
        return Ok(result);
    }

    [HttpGet("board/{boardId:int}")]
    public async Task<IActionResult> GetByBoardId(int boardId)
    {
        var result = await _sprintService.GetByBoardId(boardId);
        return Ok(result);
    }

    [HttpPut("{sprintId:int}")]
    public async Task<IActionResult> Update(
        int sprintId,
        [FromBody] UpdateSprintRequest request)
    {
        var result = await _sprintService.Update(sprintId, request);
        return Ok(result);
    }

    [HttpDelete("{sprintId:int}")]
    public async Task<IActionResult> Delete(int sprintId)
    {
        await _sprintService.Delete(sprintId);
        return NoContent();
    }
}
