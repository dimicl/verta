using backend.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace backend.API.Controllers;

[ApiController]
[Route("api/chat")]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;

    public ChatController(IChatService chatService)
    {
        _chatService = chatService;
    }

    [HttpPost("messages")]
    public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request)
    {
        var message = await _chatService.SendMessage(request.SenderId, request.ReceiverId, request.Content);
        return Ok(message);
    }

    [HttpGet("conversations/{conversationId:int}/messages")]
    public async Task<IActionResult> GetMessages(int conversationId, [FromQuery] int userId)
    {
        var messages = await _chatService.GetMessages(conversationId, userId);
        return Ok(messages);
    }
}

public class SendMessageRequest
{
    public int SenderId { get; set; }
    public int ReceiverId { get; set; }
    public string Content { get; set; } = string.Empty;
}
