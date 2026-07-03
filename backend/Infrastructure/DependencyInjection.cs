using backend.Application.Interfaces;
using backend.Application.Services;
using backend.Infrastructure.Messaging;
using backend.Infrastructure.Notifications;
using backend.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using backend.Infrastructure.BackgroundServices;
using backend.Infrastructure.Storage;

namespace backend.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is missing.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        //services
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IChatService, ChatService>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IWorkspaceRepository, WorkspaceRepository>();
        services.AddScoped<IWorkspaceService, WorkspaceService>();     
        services.AddScoped<IWorkspaceMemberRepository, WorkspaceMemberRepository>();
        services.AddScoped<IWorkspaceMemberService, WorkspaceMemberService>();
        services.AddScoped<IInvitationRepository, InvitationRepository>();
        services.AddScoped<IInvitationService, InvitationService>();  
        services.AddScoped<IUserContext, UserContext>(); 
        services.AddScoped<IConversationRepository, ConversationRepository>();
        services.AddScoped<IConversationParticipantRepository, ConversationParticipantRepository>();
        services.AddScoped<IMessageRepository, MessageRepository>();
        services.AddScoped<INotificationService, SignalRNotificationService>();
        services.AddSingleton<IRabbitMqConnectionManager, RabbitMqConnectionManager>();
        services.AddScoped<IMessageBus, RabbitMQBus>();
        services.AddScoped<IBoardRepository, BoardRepository>();
        services.AddScoped<IBoardMemberRepository, BoardMemberRepository>();
        services.AddScoped<IBoardAccessService, BoardAccessService>();
        services.AddScoped<IBoardMemberSyncService, BoardMemberSyncService>();
        services.AddScoped<IBoardService, BoardService>();
        services.AddScoped<ISprintRepository, SprintRepository>();
        services.AddScoped<ISprintService, SprintService>();
        services.AddScoped<IWorkItemRepository, WorkItemRepository>();
        services.AddScoped<IWorkItemService, WorkItemService>();
        services.AddScoped<ICommentRepository, CommentRepository>();
        services.AddScoped<ICommentService, CommentService>();
        services.AddScoped<ISubWorkItemRepository, SubWorkItemRepository>();
        services.AddScoped<ISubWorkItemService, SubWorkItemService>();
        services.AddScoped<IWorkItemFileRepository, WorkItemFileRepository>();
        services.AddScoped<IFileStorageService, LocalFileStorageService>();
        services.AddScoped<IWorkItemFileService, WorkItemFileService>();
        services.AddScoped<DomainEventSubject>();
        services.AddScoped<IDomainEventObserver, SignalRDomainEventObserver>();
        services.AddScoped<IDomainEventObserver, RabbitMqDomainEventObserver>();
        services.AddScoped<IBoardLockRepository, BoardLockRepository>();
        services.AddScoped<IBoardLockQueueRepository, BoardLockQueueRepository>();
        services.AddHostedService<BoardLockExpiryService>();
        services.AddScoped<BoardLockService>();
        services.AddScoped<IBoardLockService>(sp => sp.GetRequiredService<BoardLockService>());
        services.AddScoped<IBoardLockPromotionService>(sp => sp.GetRequiredService<BoardLockService>());
        services.AddScoped<IWorkItemLockRepository, WorkItemLockRepository>();
        services.AddScoped<IWorkItemLockInterestRepository, WorkItemLockInterestRepository>();
        services.AddScoped<WorkItemLockService>();
        services.AddScoped<IWorkItemLockService>(sp => sp.GetRequiredService<WorkItemLockService>());
        services.AddScoped<IWorkItemLockExpiryService>(sp => sp.GetRequiredService<WorkItemLockService>());
        services.AddHostedService<WorkItemLockExpiryService>();
        services.AddScoped<CommandInvoker>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IDomainEventLogRepository, DomainEventLogRepository>();
        services.AddHostedService<DomainEventConsumerService>();
        services.AddHostedService<UserEventConsumerService>();

        services.AddHttpContextAccessor();

        return services;
    }
}
