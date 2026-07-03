using Microsoft.EntityFrameworkCore;

namespace backend.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<ConversationParticipant> ConversationParticipants => Set<ConversationParticipant>();
    public DbSet<Message> Messages => Set<Message>();

    public DbSet<Workspace> Workspaces => Set<Workspace>();
    public DbSet<WorkspaceMember> WorkspaceMembers => Set<WorkspaceMember>();
    public DbSet<Invitation> Invitations => Set<Invitation>();

    public DbSet<Board> Boards => Set<Board>();
    public DbSet<BoardMember> BoardMembers => Set<BoardMember>();
    public DbSet<Sprint> Sprints => Set<Sprint>();
    public DbSet<WorkItem> WorkItems => Set<WorkItem>();
    public DbSet<SubWorkItem> SubWorkItems => Set<SubWorkItem>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<WorkItemFile> WorkItemFiles => Set<WorkItemFile>();

    public DbSet<BoardLock> BoardLocks => Set<BoardLock>();
    public DbSet<BoardLockQueueEntry> BoardLockQueueEntries => Set<BoardLockQueueEntry>();
    public DbSet<WorkItemLock> WorkItemLocks => Set<WorkItemLock>();
    public DbSet<WorkItemLockInterest> WorkItemLockInterests => Set<WorkItemLockInterest>();

    public DbSet<DomainEventLog> DomainEventLogs => Set<DomainEventLog>();
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.FirstName).IsRequired();
            entity.Property(e => e.LastName).IsRequired();
            entity.Property(e => e.Email).IsRequired();
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Password).IsRequired();
            entity.Property(e => e.Status).IsRequired().HasConversion<string>();
            entity.Property(e => e.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();
            entity.Property(e => e.UpdatedAt).HasColumnType("timestamp with time zone");
            entity.Property(e => e.IsOnline).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.LastSeenAt).HasColumnType("timestamp with time zone");
        });

        modelBuilder.Entity<Conversation>(entity =>
        {
            entity.ToTable("conversations");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Type).IsRequired().HasConversion<string>();
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();
            entity.HasOne(e => e.CreatedByUser)
                .WithMany()
                .HasForeignKey(e => e.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ConversationParticipant>(entity =>
        {
            entity.ToTable("conversation_participants");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.JoinedAt).HasColumnType("timestamp with time zone");
            entity.Property(e => e.LastReadAt).HasColumnType("timestamp with time zone");
            entity.HasOne(e => e.Conversation)
                .WithMany(c => c.Participants)
                .HasForeignKey(e => e.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.UserId);
        });

        modelBuilder.Entity<Message>(entity =>
        {
            entity.ToTable("messages");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Content).IsRequired();
            entity.Property(e => e.IsEdited).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.IsDeleted).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();
            entity.Property(e => e.EditedAt).HasColumnType("timestamp with time zone");
            entity.Property(e => e.DeletedAt).HasColumnType("timestamp with time zone");
            entity.HasOne<Conversation>()
                .WithMany()
                .HasForeignKey(e => e.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(e => e.SenderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Workspace>(entity =>
        {
            entity.ToTable("workspaces");

            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedOnAdd();

            entity.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(x => x.CreatedAt)
                .IsRequired();

            entity.HasOne(x => x.Owner)
                .WithMany()
                .HasForeignKey(x => x.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasIndex(x => x.OwnerId).IsUnique();
        });


        modelBuilder.Entity<WorkspaceMember>(entity =>
        {
            entity.ToTable("workspace_members");

            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedOnAdd();

            entity.Property(x => x.Role)
                .IsRequired().HasConversion<string>();

            entity.Property(x => x.CreatedAt)
                .IsRequired();

            entity.HasOne(x => x.Workspace)
                .WithMany(w => w.Members)
                .HasForeignKey(x => x.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(x => new { x.WorkspaceId, x.UserId })
                .IsUnique();
        });

        modelBuilder.Entity<Invitation>(entity =>
        {
            entity.ToTable("invitations");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id)
                .ValueGeneratedOnAdd();

            entity.Property(x => x.Role)
                .IsRequired()
                .HasConversion<string>();

            entity.Property(x => x.IsAccepted)
                .IsRequired();

            entity.HasOne(x => x.Workspace)
                .WithMany()
                .HasForeignKey(x => x.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Board>(entity =>
        {
            entity.ToTable("boards");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(x => x.CreatedAt)
                .IsRequired();

            entity.HasOne(x => x.Workspace)
                .WithMany(w => w.Boards)
                .HasForeignKey(x => x.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Owner)
                .WithMany()
                .HasForeignKey(x => x.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<BoardMember>(entity =>
        {
            entity.ToTable("board_members");

            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedOnAdd();

            entity.Property(x => x.CreatedAt)
                .IsRequired();

            entity.HasOne(x => x.Board)
                .WithMany()
                .HasForeignKey(x => x.BoardId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(x => new { x.BoardId, x.UserId })
                .IsUnique();
        });

        modelBuilder.Entity<Sprint>(entity =>
        {
            entity.ToTable("sprints");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(x => x.StartDate)
                .HasColumnType("timestamp with time zone");

            entity.Property(x => x.EndDate)
                .HasColumnType("timestamp with time zone");

            entity.Property(x => x.CreatedAt)
                .IsRequired();

            entity.HasOne(x => x.Board)
                .WithMany(b => b.Sprints)
                .HasForeignKey(x => x.BoardId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<BoardLock>(entity =>
        {
            entity.ToTable("board_locks");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.LockedAt)
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            entity.Property(x => x.ExpiresAt)
                .HasColumnType("timestamp with time zone")
                .IsRequired()
                .HasDefaultValueSql("NOW() + INTERVAL '30 seconds'");

            entity.HasOne(x => x.Board)
                .WithMany()
                .HasForeignKey(x => x.BoardId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.LockedByUser)
                .WithMany()
                .HasForeignKey(x => x.LockedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(x => x.BoardId)
                .IsUnique();
        });

        modelBuilder.Entity<BoardLockQueueEntry>(entity =>
        {
            entity.ToTable("board_lock_queue");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedOnAdd();
            entity.Property(x => x.JoinedAt)
                .HasColumnType("timestamp with time zone")
                .IsRequired();
            entity.HasOne(x => x.Board)
                .WithMany()
                .HasForeignKey(x => x.BoardId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(x => new { x.BoardId, x.UserId })
                .IsUnique();
            entity.HasIndex(x => new { x.BoardId, x.JoinedAt });
        });

        modelBuilder.Entity<WorkItem>(entity =>
        {
            entity.ToTable("work_items");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(x => x.Description)
                .IsRequired();

            entity.Property(x => x.Status)
                .HasConversion<string>();

            entity.Property(x => x.Priority)
                .HasConversion<string>();

            entity.Property(x => x.CreatedAt)
                .IsRequired();

            entity.HasOne(x => x.Board)
                .WithMany(b => b.WorkItems)
                .HasForeignKey(x => x.BoardId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.CreatedByUser)
                .WithMany()
                .HasForeignKey(x => x.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.AssignedUser)
                .WithMany()
                .HasForeignKey(x => x.AssignedUserId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(x => x.Sprint)
                .WithMany(s => s.WorkItems)
                .HasForeignKey(x => x.SprintId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<WorkItemLock>(entity =>
        {
            entity.ToTable("work_item_locks");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.LockedAt)
                .HasColumnType("timestamp with time zone")
                .IsRequired();
            entity.Property(x => x.ExpiresAt)
                .HasColumnType("timestamp with time zone")
                .IsRequired();
            entity.HasOne(x => x.WorkItem)
                .WithMany()
                .HasForeignKey(x => x.WorkItemId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.LockedByUser)
                .WithMany()
                .HasForeignKey(x => x.LockedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(x => x.WorkItemId)
                .IsUnique();
        });

        modelBuilder.Entity<WorkItemLockInterest>(entity =>
        {
            entity.ToTable("work_item_lock_interests");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.RegisteredAt)
                .HasColumnType("timestamp with time zone")
                .IsRequired();
            entity.HasOne(x => x.WorkItem)
                .WithMany()
                .HasForeignKey(x => x.WorkItemId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(x => new { x.WorkItemId, x.UserId })
                .IsUnique();
        });

        modelBuilder.Entity<SubWorkItem>(entity =>
        {
            entity.ToTable("sub_work_items");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Name)
                .IsRequired();

            entity.Property(x => x.Description)
                .IsRequired();

            entity.Property(x => x.Status)
                .HasConversion<string>();

            entity.HasOne(x => x.WorkItem)
                .WithMany(w => w.SubWorkItems)
                .HasForeignKey(x => x.WorkItemId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Comment>(entity =>
        {
            entity.ToTable("comments");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Content)
                .IsRequired();

            entity.Property(x => x.CreatedAt)
                .IsRequired();

            entity.HasOne(x => x.WorkItem)
                .WithMany(w => w.Comments)
                .HasForeignKey(x => x.WorkItemId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<WorkItemFile>(entity =>
        {
            entity.ToTable("work_item_files");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.FileName)
                .IsRequired();

            entity.Property(x => x.FileType)
                .IsRequired();

            entity.Property(x => x.FileUrl)
                .IsRequired();

            entity.Property(x => x.CreatedAt)
                .IsRequired();

            entity.HasOne(x => x.WorkItem)
                .WithMany(w => w.Files)
                .HasForeignKey(x => x.WorkItemId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DomainEventLog>(entity =>
        {
            entity.ToTable("domain_event_logs");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedOnAdd();
            entity.Property(x => x.EventName).IsRequired().HasMaxLength(100);
            entity.Property(x => x.Payload).IsRequired();
            entity.Property(x => x.QueueName).IsRequired().HasMaxLength(100);
            entity.Property(x => x.ReceivedAt)
                .HasColumnType("timestamp with time zone")
                .IsRequired();
            entity.HasIndex(x => x.EventName);
            entity.HasIndex(x => x.ReceivedAt);
        });
    }
}
