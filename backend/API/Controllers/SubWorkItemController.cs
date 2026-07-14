using backend.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Authorize]
[Route("api/sub-work-items")]
public class SubWorkItemController : ControllerBase
{
    private readonly ISubWorkItemService _subWorkItemService;

    public SubWorkItemController(ISubWorkItemService subWorkItemService)
    {
        _subWorkItemService = subWorkItemService;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SubWorkItemRequest request)
    {
        var result = await _subWorkItemService.Create(request);
        return Ok(result);
    }

    [HttpGet("{subWorkItemId:int}")]
    public async Task<IActionResult> GetById(int subWorkItemId)
    {
        var result = await _subWorkItemService.GetById(subWorkItemId);
        return Ok(result);
    }

    [HttpGet("work-item/{workItemId:int}")]
    public async Task<IActionResult> GetByWorkItemId(int workItemId)
    {
        var result = await _subWorkItemService.GetByWorkItemId(workItemId);
        return Ok(result);
    }

    [HttpPut("{subWorkItemId:int}")]
    public async Task<IActionResult> Update(
        int subWorkItemId,
        [FromBody] UpdateSubWorkItemRequest request)
    {
        var result = await _subWorkItemService.Update(subWorkItemId, request);
        return Ok(result);
    }

    [HttpPatch("{subWorkItemId:int}/status")]
    public async Task<IActionResult> ChangeStatus(
        int subWorkItemId,
        [FromBody] ChangeSubWorkItemStatusRequest request)
    {
        var result = await _subWorkItemService.ChangeStatus(subWorkItemId, request);
        return Ok(result);
    }

    [HttpDelete("{subWorkItemId:int}")]
    public async Task<IActionResult> Delete(int subWorkItemId)
    {
        await _subWorkItemService.Delete(subWorkItemId);
        return NoContent();
    }
}