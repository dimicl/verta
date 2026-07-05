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
        try
        {
            var result = await _subWorkItemService.Create(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{subWorkItemId:int}")]
    public async Task<IActionResult> GetById(int subWorkItemId)
    {
        try
        {
            var result = await _subWorkItemService.GetById(subWorkItemId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("work-item/{workItemId:int}")]
    public async Task<IActionResult> GetByWorkItemId(int workItemId)
    {
        try
        {
            var result = await _subWorkItemService.GetByWorkItemId(workItemId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{subWorkItemId:int}")]
    public async Task<IActionResult> Update(
        int subWorkItemId,
        [FromBody] UpdateSubWorkItemRequest request)
    {
        try
        {
            var result = await _subWorkItemService.Update(subWorkItemId, request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPatch("{subWorkItemId:int}/status")]
    public async Task<IActionResult> ChangeStatus(
        int subWorkItemId,
        [FromBody] ChangeSubWorkItemStatusRequest request)
    {
        try
        {
            var result = await _subWorkItemService.ChangeStatus(subWorkItemId, request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}