using backend.Application.Exceptions;
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
    [HttpPost("workspace/invite")]
    public async Task<IActionResult> InviteUser(
        [FromBody] InvitationRequest request)
    {
        try
        {
            var result = await invitationService.InviteUser(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            
            return BadRequest(new {message = ex.Message});
        }
        
    }

    [Authorize]
    [HttpGet("workspace/{workspaceId:int}/members")]
    public async Task<IActionResult> GetMembers(int workspaceId)
    {
        try
        {
            var result = await memberService.GetMembersByWorkspaceId(workspaceId);
            return Ok(result);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ForbiddenException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
        }
    }
}