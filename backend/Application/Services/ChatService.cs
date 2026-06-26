using backend.Application.Exceptions;
using backend.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace backend.Application.Services;

public class ChatService : IChatService
{
    private const int DefaultPageSize = 50;

    private readonly IConversationRepository _conversationRepository;
    private readonly IConversationParticipantRepository _participantRepository;
    private readonly IMessageRepository _messageRepository;
    private readonly INotificationService _notificationService;
    private readonly IWorkspaceMemberRepository _workspaceMemberRepo;

    public ChatService(
        IConversationRepository conversationRepository,
        IConversationParticipantRepository participantRepository,
        IMessageRepository messageRepository,
        INotificationService notificationService,
        IWorkspaceMemberRepository workspaceMemberRepo)
    {
        _conversationRepository = conversationRepository;
        _participantRepository = participantRepository;
        _messageRepository = messageRepository;
        _notificationService = notificationService;
        _workspaceMemberRepo = workspaceMemberRepo;
    }

    public async Task<MessageResponse> SendMessage(int senderId, int receiverId, string content)
    {
        var conversation = await _conversationRepository.GetDirectConversation(senderId, receiverId);

        var senderWorkspaces = await _workspaceMemberRepo.GetByUserIdAsync(senderId);
        var senderWorkspaceIds = senderWorkspaces.Select(m => m.WorkspaceId).ToHashSet();
        
        var receiverWorkspaces = await _workspaceMemberRepo.GetByUserIdAsync(receiverId);
        var hasCommonWorkspace = receiverWorkspaces.Any(m => senderWorkspaceIds.Contains(m.WorkspaceId));
        
        if (!hasCommonWorkspace)
            throw new ForbiddenException("You can only message users in your workspace.");

        if (conversation == null)
        {
            conversation = new Conversation
            {
                Type = ConversationType.Direct,
                CreatedByUserId = senderId,
                CreatedAt = DateTime.UtcNow,
                Participants = new List<ConversationParticipant>()
            };
            try
            {
            await _conversationRepository.Add(conversation);

            await _participantRepository.Add(new ConversationParticipant
            {
                ConversationId = conversation.Id,
                UserId = senderId,
                JoinedAt = DateTime.UtcNow
            });
            await _participantRepository.Add(new ConversationParticipant
            {
                ConversationId = conversation.Id,
                UserId = receiverId,
                JoinedAt = DateTime.UtcNow
            });
            }
            catch (DbUpdateException)
            {
                conversation = await _conversationRepository.GetDirectConversation(senderId, receiverId);
                if (conversation == null) throw;
            }
        }

        var isSenderParticipant = await _participantRepository.IsParticipant(conversation.Id, senderId);
        if (!isSenderParticipant)
            throw new InvalidOperationException("Sender is not a participant in this conversation.");

        var message = new Message
        {
            ConversationId = conversation.Id,
            SenderId = senderId,
            Content = content,
            CreatedAt = DateTime.UtcNow
        };

        await _messageRepository.Add(message);

        var participantUserIds = await _participantRepository.GetUserIds(conversation.Id);
        await _notificationService.SendChatMessageAsync(participantUserIds, message);

        return ToResponse(message);
    }

    public async Task<List<MessageResponse>> GetMessages(int conversationId, int userId, int? before, int limit)
    {
        var isParticipant = await _participantRepository.IsParticipant(conversationId, userId);
        if (!isParticipant)
            throw new InvalidOperationException("User is not a participant in this conversation.");

        var messages = await _messageRepository.GetMessagesByConversationId(
            conversationId, before, limit > 0 ? limit : DefaultPageSize);

        return messages.Select(ToResponse).ToList();
    }

    public Task<bool> IsUserInConversation(int conversationId, int userId)
    {
        return _participantRepository.IsParticipant(conversationId, userId);
    }

    private static MessageResponse ToResponse(Message message) => new()
    {
        Id = message.Id,
        ConversationId = message.ConversationId,
        SenderId = message.SenderId,
        Content = message.Content,
        IsEdited = message.IsEdited,
        IsDeleted = message.IsDeleted,
        CreatedAt = message.CreatedAt,
        EditedAt = message.EditedAt
    };

    public async Task MarkAsRead(int conversationId, int userId)
    {
        var isParticipant = await _participantRepository.IsParticipant(conversationId, userId);
        if (!isParticipant)
            throw new InvalidOperationException("User is not a participant in this conversation.");

        var participant = await _participantRepository.GetParticipantAsync(conversationId, userId);
        if (participant == null) return;

        var latestMessageId = await _messageRepository.GetLatestMessageIdAsync(conversationId);
        if (latestMessageId == null) return;

        participant.LastReadMessageId = latestMessageId;
        participant.LastReadAt = DateTime.UtcNow;
        await _participantRepository.Update(participant);
    }

    public async Task<int> GetUnreadCount(int conversationId, int userId)
    {
        var isParticipant = await _participantRepository.IsParticipant(conversationId, userId);
        if (!isParticipant)
            throw new InvalidOperationException("User is not a participant in this conversation.");

        var participant = await _participantRepository.GetParticipantAsync(conversationId, userId);
        if (participant == null)
            throw new InvalidOperationException("User is not a participant in this conversation.");
        return await _messageRepository.GetUnreadCountAsync(conversationId, participant?.LastReadMessageId);
    }

    public async Task<List<ConversationResponse>> GetMyConversations(int userId)
    {
        var conversations = await _conversationRepository.GetByUserIdAsync(userId);
        var result = new List<ConversationResponse>();
        foreach (var c in conversations)
        {
            var participant = await _participantRepository.GetParticipantAsync(c.Id, userId);
            var unread = participant != null
                ? await _messageRepository.GetUnreadCountAsync(c.Id, participant.LastReadMessageId)
                : 0;
            result.Add(new ConversationResponse
            {
                Id = c.Id,
                Type = c.Type.ToString(),
                Name = c.Name,
                CreatedAt = c.CreatedAt,
                UnreadCount = unread
            });
        }
        return result;
    }
}