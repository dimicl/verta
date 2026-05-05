using backend.Application.Interfaces;

namespace backend.Application.Services;

public class ChatService : IChatService
{
    private readonly IConversationRepository _conversationRepository;
    private readonly IConversationParticipantRepository _participantRepository;
    private readonly IMessageRepository _messageRepository;
    private readonly IMessageBus _messageBus;
    private readonly INotificationService _notificationService;

    public ChatService(
        IConversationRepository conversationRepository,
        IConversationParticipantRepository participantRepository,
        IMessageRepository messageRepository,
        IMessageBus messageBus,
        INotificationService notificationService)
    {
        _conversationRepository = conversationRepository;
        _participantRepository = participantRepository;
        _messageRepository = messageRepository;
        _messageBus = messageBus;
        _notificationService = notificationService;
    }

    public async Task<Message> SendMessage(int senderId, int receiverId, string content)
    {
        var conversation = await _conversationRepository.GetDirectConversation(senderId, receiverId);
        if (conversation == null)
        {
            conversation = new Conversation
            {
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

        var isSenderParticipant = await _participantRepository.IsParticipant(conversation.Id, senderId);
        if (!isSenderParticipant)
        {
            throw new InvalidOperationException("Sender is not a participant in this conversation.");
        }

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
        await _messageBus.PublishAsync(
            "chat-events",
            $"conversation:{conversation.Id}|sender:{senderId}|content:{content}");

        return message;
    }

    public async Task<List<Message>> GetMessages(int conversationId, int userId)
    {
        var isParticipant = await _participantRepository.IsParticipant(conversationId, userId);
        if (!isParticipant)
        {
            throw new InvalidOperationException("User is not a participant in this conversation.");
        }

        return await _messageRepository.GetMessagesByConversationId(conversationId);
    }

    public Task<bool> IsUserInConversation(int conversationId, int userId)
    {
        return _participantRepository.IsParticipant(conversationId, userId);
    }
}
