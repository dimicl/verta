using backend.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api")]
public class WorkspaceController : ControllerBase
{
    private readonly IWorkspaceService service;
    private readonly IWorkspaceMemberService memberService;
    public WorkspaceController(IWorkspaceService _service, IWorkspaceMemberService _memberService)
    {
        service = _service;
        memberService = _memberService;
    }

    [Authorize]
    [HttpPost("workspace")]
    public async Task<IActionResult> CreateWorkspace([FromBody] WorkspaceRequest request)
    {       
        var result = await service.Create(request);
        return Ok(result);
    }

    [Authorize]
    [HttpGet("space")]
    public async Task<IActionResult> GetWorkspace()
    {       
        var result = await service.GetByOwnerId();
        return Ok(result);
    }
}