using backend.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Authorize]
[Route("api/work-items")]
public class WorkItemController : ControllerBase
{
    private readonly IWorkItemService _workItemService;

    public WorkItemController(IWorkItemService workItemService)
    {
        _workItemService = workItemService;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] WorkItemRequest request)
    {
        try
        {
            var result = await _workItemService.Create(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{workItemId:int}")]
    public async Task<IActionResult> GetById(int workItemId)
    {
        try
        {
            var result = await _workItemService.GetById(workItemId);
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
            var result = await _workItemService.GetByBoardId(boardId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPatch("{workItemId:int}/status")]
    public async Task<IActionResult> ChangeStatus(
        int workItemId,
        [FromBody] ChangeWorkItemStatusRequest request)
    {
        try
        {
            var result = await _workItemService.ChangeStatus(workItemId, request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}