using backend.Shared.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace backend.Infrastructure.Persistence;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(
        AppDbContext db,
        IConfiguration configuration,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var enabled = configuration.GetValue("Seed:Enabled", false);
        if (!enabled)
            return;

        var demoEmail = configuration["Seed:DemoEmail"] ?? "demo@verta.com";
        var demoPassword = configuration["Seed:DemoPassword"] ?? "Demo1234!";
        var memberEmail = configuration["Seed:MemberEmail"] ?? "member@verta.com";

        if (await db.Users.AnyAsync(u => u.Email == demoEmail, cancellationToken))
        {
            logger.LogInformation("Demo data already exists, skipping seed.");
            return;
        }

        var now = DateTime.UtcNow;

        var demoUser = new User
        {
            FirstName = "Demo",
            LastName = "User",
            Email = demoEmail,
            Password = PasswordHasher.Hash(demoPassword),
            Status = UserStatus.Active,
            CreatedAt = now
        };

        var memberUser = new User
        {
            FirstName = "Team",
            LastName = "Member",
            Email = memberEmail,
            Password = PasswordHasher.Hash(demoPassword),
            Status = UserStatus.Active,
            CreatedAt = now
        };

        db.Users.AddRange(demoUser, memberUser);
        await db.SaveChangesAsync(cancellationToken);

        var workspace = new Workspace
        {
            Name = "Demo Workspace",
            OwnerId = demoUser.Id,
            CreatedAt = now
        };
        db.Workspaces.Add(workspace);
        await db.SaveChangesAsync(cancellationToken);

        db.WorkspaceMembers.AddRange(
            new WorkspaceMember
            {
                WorkspaceId = workspace.Id,
                UserId = demoUser.Id,
                Role = UserRole.Owner,
                CreatedAt = now
            },
            new WorkspaceMember
            {
                WorkspaceId = workspace.Id,
                UserId = memberUser.Id,
                Role = UserRole.Member,
                CreatedAt = now
            });

        var board = new Board
        {
            Name = "Main Board",
            WorkspaceId = workspace.Id,
            OwnerId = demoUser.Id,
            CreatedAt = now
        };
        db.Boards.Add(board);
        await db.SaveChangesAsync(cancellationToken);

        db.BoardMembers.AddRange(
            new BoardMember { BoardId = board.Id, UserId = demoUser.Id, CreatedAt = now },
            new BoardMember { BoardId = board.Id, UserId = memberUser.Id, CreatedAt = now });

        var sprint = new Sprint
        {
            Name = "Sprint 1",
            BoardId = board.Id,
            StartDate = now.Date,
            EndDate = now.Date.AddDays(14),
            CreatedAt = now
        };
        db.Sprints.Add(sprint);
        await db.SaveChangesAsync(cancellationToken);

        var setupTask = new WorkItem
        {
            Name = "Setup project",
            Description = "Initialize repository and CI pipeline.",
            Status = WorkItemStatus.Done,
            Priority = Priority.High,
            BoardId = board.Id,
            SprintId = sprint.Id,
            CreatedByUserId = demoUser.Id,
            AssignedUserId = demoUser.Id,
            CreatedAt = now
        };

        var authTask = new WorkItem
        {
            Name = "Implement authentication",
            Description = "Add login, registration and JWT handling.",
            Status = WorkItemStatus.InProgress,
            Priority = Priority.Medium,
            BoardId = board.Id,
            SprintId = sprint.Id,
            CreatedByUserId = demoUser.Id,
            AssignedUserId = memberUser.Id,
            CreatedAt = now
        };

        var backlogTask = new WorkItem
        {
            Name = "Write E2E tests",
            Description = "Cover main user flows with Playwright.",
            Status = WorkItemStatus.ToDo,
            Priority = Priority.Low,
            BoardId = board.Id,
            CreatedByUserId = demoUser.Id,
            CreatedAt = now
        };

        db.WorkItems.AddRange(setupTask, authTask, backlogTask);
        await db.SaveChangesAsync(cancellationToken);

        db.SubWorkItems.Add(new SubWorkItem
        {
            Name = "Add password hashing",
            Description = "Use PBKDF2 for stored passwords.",
            Status = WorkItemStatus.Done,
            Priority = Priority.Medium,
            WorkItemId = authTask.Id,
            UserId = demoUser.Id,
            AssignedUserId = demoUser.Id,
            CreatedAt = now
        });

        db.Comments.AddRange(
            new Comment
            {
                Content = "Project setup is complete.",
                WorkItemId = setupTask.Id,
                UserId = demoUser.Id,
                CreatedAt = now
            },
            new Comment
            {
                Content = "JWT middleware is working on the API.",
                WorkItemId = authTask.Id,
                UserId = memberUser.Id,
                CreatedAt = now
            });

        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Demo data seeded. Login with {Email} / {Password}. Second user: {MemberEmail}.",
            demoEmail,
            demoPassword,
            memberEmail);
    }
}
