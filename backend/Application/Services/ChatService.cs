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
        return await _messageRepository.GetUnreadCountAsync(
            conversationId,
            userId,
            participant.LastReadMessageId);
    }

    public async Task<List<ConversationResponse>> GetMyConversations(int userId)
    {
        var conversations = await _conversationRepository.GetByUserIdAsync(userId);
        var result = new List<ConversationResponse>();
        foreach (var c in conversations)
        {
            var participant = await _participantRepository.GetParticipantAsync(c.Id, userId);
            var unread = participant != null
                ? await _messageRepository.GetUnreadCountAsync(
                    c.Id,
                    userId,
                    participant.LastReadMessageId)
                : 0;

            var participants = c.Participants.Select(p => new ConversationParticipantResponse
            {
                Id = p.Id,
                UserId = p.UserId,
                FirstName = p.User?.FirstName ?? string.Empty,
                LastName = p.User?.LastName ?? string.Empty,
                IsOnline = p.User?.IsOnline ?? false
            }).ToList();

            result.Add(new ConversationResponse
            {
                Id = c.Id,
                Type = c.Type.ToString(),
                Name = c.Name,
                CreatedAt = c.CreatedAt,
                UnreadCount = unread,
                Participants = participants
            });
        }

        return result;
    }

    public async Task<int> GetOrCreateDirectConversationId(int senderId, int receiverId)
    {
        var conversation = await _conversationRepository.GetDirectConversation(senderId, receiverId);
        
        if (conversation == null)
        {
            conversation = new Conversation
            {
                Type = ConversationType.Direct,
                CreatedByUserId = senderId,
                CreatedAt = DateTime.UtcNow,
                Participants = new List<ConversationParticipant>()
            };

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

        return conversation.Id;
    }

    public async Task<MessageResponse> SendMessageToConversation(int senderId, int conversationId, string content)
    {
        var conversation = await _conversationRepository.GetByIdWithParticipants(conversationId);
        if (conversation == null)
            throw new NotFoundException("Conversation not found.");

        var isSenderParticipant = await _participantRepository.IsParticipant(conversationId, senderId);
        if (!isSenderParticipant)
            throw new ForbiddenException("You are not a participant in this conversation.");

        var message = new Message
        {
            ConversationId = conversationId,
            SenderId = senderId,
            Content = content,
            CreatedAt = DateTime.UtcNow
        };

        await _messageRepository.Add(message);

        var participantUserIds = await _participantRepository.GetUserIds(conversationId);
        await _notificationService.SendChatMessageAsync(participantUserIds, message);

        return ToResponse(message);
    }

    public async Task<ConversationResponse> CreateGroupConversation(int creatorId, string name, List<int> memberIds)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Group name is required.");

        var uniqueMemberIds = memberIds
            .Where(id => id != creatorId)
            .Distinct()
            .ToList();

        var creatorWorkspaces = await _workspaceMemberRepo.GetByUserIdAsync(creatorId);
        var creatorWorkspaceIds = creatorWorkspaces.Select(m => m.WorkspaceId).ToHashSet();

        foreach (var memberId in uniqueMemberIds)
        {
            var memberWorkspaces = await _workspaceMemberRepo.GetByUserIdAsync(memberId);
            var hasCommonWorkspace = memberWorkspaces.Any(m => creatorWorkspaceIds.Contains(m.WorkspaceId));
            if (!hasCommonWorkspace)
                throw new ForbiddenException("You can only add users from your workspace to a group.");
        }

        var conversation = new Conversation
        {
            Type = ConversationType.Group,
            Name = name.Trim(),
            CreatedByUserId = creatorId,
            CreatedAt = DateTime.UtcNow,
            Participants = new List<ConversationParticipant>()
        };

        await _conversationRepository.Add(conversation);

        var allMemberIds = new List<int> { creatorId };
        allMemberIds.AddRange(uniqueMemberIds);

        foreach (var memberId in allMemberIds)
        {
            await _participantRepository.Add(new ConversationParticipant
            {
                ConversationId = conversation.Id,
                UserId = memberId,
                JoinedAt = DateTime.UtcNow
            });
        }

        var created = await _conversationRepository.GetByIdWithParticipants(conversation.Id);
        if (created == null)
            throw new InvalidOperationException("Failed to create group conversation.");

        var participants = created.Participants.Select(p => new ConversationParticipantResponse
        {
            Id = p.Id,
            UserId = p.UserId,
            FirstName = p.User?.FirstName ?? string.Empty,
            LastName = p.User?.LastName ?? string.Empty,
            IsOnline = p.User?.IsOnline ?? false
        }).ToList();

        return new ConversationResponse
        {
            Id = created.Id,
            Type = created.Type.ToString(),
            Name = created.Name,
            CreatedAt = created.CreatedAt,
            UnreadCount = 0,
            Participants = participants
        };
    }
}