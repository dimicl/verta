using backend.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Authorize]
[Route("api/work-item-files")]
public class WorkItemFileController : ControllerBase
{
    private readonly IWorkItemFileService _fileService;

    public WorkItemFileController(IWorkItemFileService fileService)
    {
        _fileService = fileService;
    }

    [HttpPost("upload")]
    [RequestSizeLimit(104_857_600)]
    public async Task<IActionResult> Upload(
         int workItemId,
         IFormFile file,
         int? subWorkItemId = null)
    {
        var result = await _fileService.Upload(workItemId, file, subWorkItemId);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] WorkItemFileRequest request)
    {
        var result = await _fileService.Create(request);
        return Ok(result);
    }

    [HttpGet("work-item/{workItemId:int}")]
    public async Task<IActionResult> GetByWorkItemId(int workItemId)
    {
        var result = await _fileService.GetByWorkItemId(workItemId);
        return Ok(result);
    }

    [HttpGet("sub-work-item/{subWorkItemId:int}")]
    public async Task<IActionResult> GetBySubWorkItemId(int subWorkItemId)
    {
        var result = await _fileService.GetBySubWorkItemId(subWorkItemId);
        return Ok(result);
    }

    [HttpDelete("{fileId:int}")]
    public async Task<IActionResult> Delete(int fileId)
    {
        await _fileService.Delete(fileId);
        return Ok(new { message = "File deleted successfully." });
    }
}