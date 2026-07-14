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
        var result = await _workItemService.Create(request);
        return Ok(result);
    }

    [HttpGet("{workItemId:int}")]
    public async Task<IActionResult> GetById(int workItemId)
    {
        var result = await _workItemService.GetById(workItemId);
        return Ok(result);
    }

    [HttpGet("board/{boardId:int}")]
    public async Task<IActionResult> GetByBoardId(int boardId)
    {
        var result = await _workItemService.GetByBoardId(boardId);
        return Ok(result);
    }

    [HttpGet("sprint/{sprintId:int}")]
    public async Task<IActionResult> GetBySprintId(int sprintId)
    {
        var result = await _workItemService.GetBySprintId(sprintId);
        return Ok(result);
    }

    [HttpPatch("{workItemId:int}/status")]
    public async Task<IActionResult> ChangeStatus(
        int workItemId,
        [FromBody] ChangeWorkItemStatusRequest request)
    {
        var result = await _workItemService.ChangeStatus(workItemId, request);
        return Ok(result);
    }

    [HttpPatch("{workItemId:int}/priority")]
    public async Task<IActionResult> ChangePriority(
        int workItemId,
        [FromBody] ChangeWorkItemPriorityRequest request)
    {
        var result = await _workItemService.ChangePriority(workItemId, request);
        return Ok(result);
    }

    [HttpPatch("{workItemId:int}/assignee")]
    public async Task<IActionResult> ChangeAssignee(
        int workItemId,
        [FromBody] ChangeWorkItemAssigneeRequest request)
    {
        var result = await _workItemService.ChangeAssignee(workItemId, request);
        return Ok(result);
    }

    [HttpPut("{workItemId:int}")]
    public async Task<IActionResult> Update(
        int workItemId,
        [FromBody] WorkItemRequest request)
    {
        var result = await _workItemService.Update(workItemId, request);
        return Ok(result);
    }

    [HttpDelete("{workItemId:int}")]
    public async Task<IActionResult> Delete(int workItemId)
    {
        await _workItemService.Delete(workItemId);
        return NoContent();
    }
}