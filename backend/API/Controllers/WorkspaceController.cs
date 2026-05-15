using backend.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api")]
public class WorkspaceController : ControllerBase
{
    private readonly IWorkspaceService service;
    private readonly IWorkspaceMemberService memberService;
    private readonly IInvitationService invitationService;
    public WorkspaceController(IWorkspaceService _service, IWorkspaceMemberService _memberService, IInvitationService _invitationService)
    {
        service = _service;
        memberService = _memberService;
        invitationService = _invitationService;
    }

    [Authorize]
    [HttpPost("workspace")]
    public async Task<IActionResult> CreateWorkspace([FromBody] WorkspaceRequest request)
    {       
        var result = await service.Create(request);
        return Ok(result);
    }

    [Authorize]
    [HttpGet("workspace/my")]
    public async Task<IActionResult> GetMyWorkspace()
    {
        var result = await service.GetByOwnerId();

        return Ok(result);
    }

    [Authorize]
    [HttpPost("workspace/{workspaceId}/invite")]
    public async Task<IActionResult> InviteUser(
        int workspaceId,
        [FromBody] InvitationRequest request)
    {
        var result = await invitationService.InviteUser(workspaceId, request);

        return Ok(result);
    }
}