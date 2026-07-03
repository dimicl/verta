using backend.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.API.Controllers;

[ApiController]
[Authorize]
[Route("api/chat")]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;
    private readonly IUserContext _userContext;

    public ChatController(IChatService chatService, IUserContext userContext)
    {
        _chatService = chatService;
        _userContext = userContext;
    }

    [HttpPost("messages")]
    public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request)
    {
        var senderId = _userContext.GetUserId();
        var message = await _chatService.SendMessage(senderId, request.ReceiverId, request.Content);
        return Ok(message);
    }

    [HttpGet("conversations/{conversationId:int}/messages")]
    public async Task<IActionResult> GetMessages(
        int conversationId,
        [FromQuery] int? before,
        [FromQuery] int limit = 50)
    {
        var userId = _userContext.GetUserId();
        var messages = await _chatService.GetMessages(conversationId, userId, before, limit);
        return Ok(messages);
    }

    [HttpPost("conversations/{conversationId:int}/read")]
    public async Task<IActionResult> MarkAsRead(int conversationId)
    {
        var userId = _userContext.GetUserId();
        await _chatService.MarkAsRead(conversationId, userId);
        return Ok();
    }

    [HttpGet("conversations/{conversationId:int}/unread")]
    public async Task<IActionResult> GetUnreadCount(int conversationId)
    {
        var userId = _userContext.GetUserId();
        var count = await _chatService.GetUnreadCount(conversationId, userId);
        return Ok(new { conversationId, unreadCount = count });
    }

    [HttpGet("conversations/my")]
    public async Task<IActionResult> GetMyConversations()
    {
        var userId = _userContext.GetUserId();
        var conversations = await _chatService.GetMyConversations(userId);
        return Ok(conversations);
    }

    [HttpGet("conversations/search")]
    public async Task<IActionResult> GetOrCreateConversation([FromQuery] int receiverId)
    {
        var senderId = _userContext.GetUserId();
        var conversationId = await _chatService.GetOrCreateDirectConversationId(senderId, receiverId);
        return Ok(new { conversationId });
    }

    [HttpPost("conversations/group")]
    public async Task<IActionResult> CreateGroupConversation([FromBody] CreateGroupConversationRequest request)
    {
        var creatorId = _userContext.GetUserId();
        var conversation = await _chatService.CreateGroupConversation(
            creatorId,
            request.Name,
            request.MemberIds ?? []);
        return Ok(conversation);
    }

    [HttpPost("conversations/{conversationId:int}/messages")]
    public async Task<IActionResult> SendMessageToConversation(
        int conversationId,
        [FromBody] SendConversationMessageRequest request)
    {
        var senderId = _userContext.GetUserId();
        var message = await _chatService.SendMessageToConversation(
            senderId,
            conversationId,
            request.Content);
        return Ok(message);
    }
    }

public class SendMessageRequest
{
    public int ReceiverId { get; set; }
    public string Content { get; set; } = string.Empty;
}

public class SendConversationMessageRequest
{
    public string Content { get; set; } = string.Empty;
}

public class CreateGroupConversationRequest
{
    public string Name { get; set; } = string.Empty;
    public List<int>? MemberIds { get; set; }
}