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
    [HttpPut("workspace/{workspaceId:int}")]
    public async Task<IActionResult> UpdateWorkspace(
        int workspaceId,
        [FromBody] WorkspaceRequest request)
    {
        var result = await service.Update(workspaceId, request);
        return Ok(result);
    }

    [Authorize]
    [HttpDelete("workspace/{workspaceId:int}")]
    public async Task<IActionResult> DeleteWorkspace(int workspaceId)
    {
        await service.Delete(workspaceId);
        return NoContent();
    }

    [Authorize]
    [HttpPost("workspace/invite")]
    public async Task<IActionResult> InviteUser(
        [FromBody] InvitationRequest request)
    {
        var result = await invitationService.InviteUser(request);
        return Ok(result);
        
    }

    [Authorize]
    [HttpGet("workspace/{workspaceId:int}/members")]
    public async Task<IActionResult> GetMembers(int workspaceId)
    {
        var result = await memberService.GetMembersByWorkspaceId(workspaceId);
        return Ok(result);
    }
}