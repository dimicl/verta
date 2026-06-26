using backend.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Authorize]
[Route("api/work-item-locks")]
public class WorkItemLockController : ControllerBase
{
    private readonly IWorkItemLockService _workItemLockService;

    public WorkItemLockController(IWorkItemLockService workItemLockService)
    {
        _workItemLockService = workItemLockService;
    }

    [HttpPost("open/{workItemId:int}")]
    public async Task<IActionResult> OpenWorkItem(int workItemId)
    {
        var result = await _workItemLockService.OpenWorkItem(workItemId);
        return Ok(result);
    }

    [HttpPost("close/{workItemId:int}")]
    public async Task<IActionResult> CloseWorkItem(int workItemId)
    {
        var result = await _workItemLockService.CloseWorkItem(workItemId);
        return Ok(result);
    }

    [HttpPost("heartbeat/{workItemId:int}")]
    public async Task<IActionResult> Heartbeat(int workItemId)
    {
        var result = await _workItemLockService.Heartbeat(workItemId);
        return Ok(result);
    }
}