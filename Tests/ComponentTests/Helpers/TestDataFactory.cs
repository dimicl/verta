namespace ComponentTests.Helpers;

public static class TestDataFactory
{
    public static DomainEventSubject EmptyEvents() =>
        new(Enumerable.Empty<IDomainEventObserver>());

    public static Board CreateBoard(int id = 1, int workspaceId = 1, int ownerId = 1) => new()
    {
        Id = id,
        Name = "Demo Board",
        WorkspaceId = workspaceId,
        OwnerId = ownerId,
        CreatedAt = DateTime.UtcNow
    };

    public static WorkItem CreateWorkItem(
        int id = 1,
        int boardId = 1,
        string name = "Test task",
        string description = "Test description") => new()
    {
        Id = id,
        Name = name,
        Description = description,
        BoardId = boardId,
        Status = WorkItemStatus.ToDo,
        Priority = Priority.Medium,
        CreatedByUserId = 1,
        CreatedAt = DateTime.UtcNow
    };

    public static WorkItemLock CreateLock(
        int workItemId = 1,
        int userId = 1,
        DateTime? lockedAt = null,
        DateTime? expiresAt = null) => new()
    {
        Id = 1,
        WorkItemId = workItemId,
        LockedByUserId = userId,
        LockedAt = lockedAt ?? DateTime.UtcNow.AddMinutes(-1),
        ExpiresAt = expiresAt ?? DateTime.UtcNow.AddSeconds(30)
    };

    public static WorkItemLockInterest CreateInterest(
        int workItemId = 1,
        int userId = 2) => new()
    {
        Id = 1,
        WorkItemId = workItemId,
        UserId = userId,
        RegisteredAt = DateTime.UtcNow
    };

    public static Comment CreateComment(
        int id = 1,
        int workItemId = 1,
        int userId = 1,
        string content = "Test comment") => new()
    {
        Id = id,
        Content = content,
        WorkItemId = workItemId,
        UserId = userId,
        CreatedAt = DateTime.UtcNow,
        User = new User
        {
            Id = userId,
            FirstName = "Test",
            LastName = "User",
            Email = "test@example.com",
            Password = "x"
        }
    };

    public static Sprint CreateSprint(int id = 1, int boardId = 1) => new()
    {
        Id = id,
        Name = "Sprint 1",
        BoardId = boardId,
        StartDate = DateTime.UtcNow.Date,
        EndDate = DateTime.UtcNow.Date.AddDays(14),
        CreatedAt = DateTime.UtcNow
    };

    public static Workspace CreateWorkspace(int id = 1, int ownerId = 1, string name = "Demo Workspace") => new()
    {
        Id = id,
        Name = name,
        OwnerId = ownerId,
        CreatedAt = DateTime.UtcNow
    };

    public static WorkspaceMember CreateMember(
        int workspaceId = 1,
        int userId = 1,
        UserRole role = UserRole.Owner) => new()
    {
        Id = 1,
        WorkspaceId = workspaceId,
        UserId = userId,
        Role = role,
        CreatedAt = DateTime.UtcNow
    };

    public static WorkItemRequest CreateWorkItemRequest(
        string name = "Test task",
        string description = "Test description",
        int boardId = 1) => new()
    {
        Name = name,
        Description = description,
        BoardId = boardId,
        Priority = Priority.Medium
    };

    public static CommentRequest CreateCommentRequest(
        int workItemId = 1,
        string content = "Test comment") => new()
    {
        WorkItemId = workItemId,
        Content = content
    };

    public static UpdateCommentRequest CreateUpdateCommentRequest(string content = "Updated comment") => new()
    {
        Content = content
    };

    public static WorkspaceRequest CreateWorkspaceRequest(string name = "Demo Workspace") => new()
    {
        Name = name
    };

    public static SprintRequest CreateSprintRequest(
        string name = "Sprint 1",
        int boardId = 1) => new()
    {
        Name = name,
        BoardId = boardId,
        StartDate = DateTime.UtcNow.Date,
        EndDate = DateTime.UtcNow.Date.AddDays(14)
    };

    public static UpdateSprintRequest CreateUpdateSprintRequest(
        string name = "Updated sprint") => new()
    {
        Name = name,
        StartDate = DateTime.UtcNow.Date,
        EndDate = DateTime.UtcNow.Date.AddDays(21)
    };

    public static List<WorkItem> CreateWorkItemEntities(params int[] ids) =>
        ids.Select(id => CreateWorkItem(id)).ToList();

    public static List<Comment> CreateCommentEntities(params int[] ids) =>
        ids.Select(id => CreateComment(id)).ToList();
}
