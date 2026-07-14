using backend.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Authorize]
[Route("api/comments")]
public class CommentController : ControllerBase
{
    private readonly ICommentService _commentService;

    public CommentController(ICommentService commentService)
    {
        _commentService = commentService;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CommentRequest request)
    {
        var result = await _commentService.Create(request);
        return Ok(result);
    }

    [HttpGet("work-item/{workItemId:int}")]
    public async Task<IActionResult> GetByWorkItemId(int workItemId)
    {
        var result = await _commentService.GetByWorkItemId(workItemId);
        return Ok(result);
    }

    [HttpGet("sub-work-item/{subWorkItemId:int}")]
    public async Task<IActionResult> GetBySubWorkItemId(int subWorkItemId)
    {
        var result = await _commentService.GetBySubWorkItemId(subWorkItemId);
        return Ok(result);
    }

    [HttpPut("{commentId:int}")]
    public async Task<IActionResult> Update(
        int commentId,
        [FromBody] UpdateCommentRequest request)
    {
        var result = await _commentService.Update(commentId, request);
        return Ok(result);
    }

    [HttpDelete("{commentId:int}")]
    public async Task<IActionResult> Delete(int commentId)
    {
        await _commentService.Delete(commentId);
        return NoContent();
    }
}