namespace ComponentTests.Helpers;

public static class TestDataFactory
{
    public static WorkItemRequest CreateWorkItemRequest() => new()
    {
        Name = "Test task",
        Description = "Test description",
        BoardId = 1,
        Priority = Priority.Medium
    };

    public static WorkItemResponse CreateWorkItemResponse(int id = 1) => new()
    {
        Id = id,
        Name = "Test task",
        Description = "Test description",
        Status = WorkItemStatus.ToDo,
        Priority = Priority.Medium,
        BoardId = 1,
        CreatedByUserId = 1,
        CreatedAt = DateTime.UtcNow
    };

    public static CommentRequest CreateCommentRequest() => new()
    {
        WorkItemId = 1,
        Content = "Test comment"
    };

    public static CommentResponse CreateCommentResponse(int id = 1) => new()
    {
        Id = id,
        Content = "Test comment",
        WorkItemId = 1,
        UserId = 1,
        FirstName = "Test",
        LastName = "User",
        CreatedAt = DateTime.UtcNow
    };

    public static UpdateCommentRequest CreateUpdateCommentRequest() => new()
    {
        Content = "Updated comment"
    };

    public static WorkspaceRequest CreateWorkspaceRequest() => new()
    {
        Name = "Demo Workspace"
    };

    public static WorkspaceResponse CreateWorkspaceResponse(int id = 1) => new()
    {
        Id = id,
        Name = "Demo Workspace",
        OwnerId = 1,
        CreatedAt = DateTime.UtcNow
    };

    public static SprintRequest CreateSprintRequest() => new()
    {
        Name = "Sprint 1",
        BoardId = 1,
        StartDate = DateTime.UtcNow.Date,
        EndDate = DateTime.UtcNow.Date.AddDays(14)
    };

    public static SprintResponse CreateSprintResponse(int id = 1) => new()
    {
        Id = id,
        Name = "Sprint 1",
        BoardId = 1,
        StartDate = DateTime.UtcNow.Date,
        EndDate = DateTime.UtcNow.Date.AddDays(14),
        CreatedAt = DateTime.UtcNow
    };

    public static UpdateSprintRequest CreateUpdateSprintRequest() => new()
    {
        Name = "Updated sprint",
        StartDate = DateTime.UtcNow.Date,
        EndDate = DateTime.UtcNow.Date.AddDays(21)
    };

    public static WorkItemLockResponse CreateWriteLockResponse(
        int workItemId = 1,
        int userId = 1,
        DateTime? lockedAt = null,
        DateTime? expiresAt = null) => new()
    {
        WorkItemId = workItemId,
        UserId = userId,
        Mode = "WRITE",
        LockedAt = lockedAt ?? DateTime.UtcNow,
        ExpiresAt = expiresAt ?? DateTime.UtcNow.AddSeconds(30)
    };

    public static WorkItemLockResponse CreateReadOnlyLockResponse(
        int workItemId = 1,
        int userId = 2,
        int queuePosition = 1) => new()
    {
        WorkItemId = workItemId,
        UserId = userId,
        Mode = "READ_ONLY",
        LockedAt = DateTime.UtcNow.AddMinutes(-1),
        ExpiresAt = DateTime.UtcNow.AddSeconds(20),
        QueuePosition = queuePosition
    };

    public static WorkItemLockResponse CreateUnlockedResponse(int workItemId = 1, int userId = 1) => new()
    {
        WorkItemId = workItemId,
        UserId = userId,
        Mode = "UNLOCKED",
        LockedAt = null,
        ExpiresAt = null
    };

    public static WorkItemLockResponse CreateNoLockResponse(int workItemId = 1, int userId = 2) => new()
    {
        WorkItemId = workItemId,
        UserId = userId,
        Mode = "NO_LOCK",
        LockedAt = null,
        ExpiresAt = null
    };

    public static List<WorkItemResponse> CreateWorkItemList(params int[] ids) =>
        ids.Select(CreateWorkItemResponse).ToList();

    public static List<CommentResponse> CreateCommentList(params int[] ids) =>
        ids.Select(CreateCommentResponse).ToList();
}
