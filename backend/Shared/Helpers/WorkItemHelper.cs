public static class WorkItemHelper
{
    public static WorkItemResponse ToResponse(WorkItem workItem)
    {
        return new WorkItemResponse
        {
            Id = workItem.Id,
            Name = workItem.Name,
            Description = workItem.Description,
            Status = workItem.Status,
            Priority = workItem.Priority,
            BoardId = workItem.BoardId,
            SprintId = workItem.SprintId,
            CreatedByUserId = workItem.CreatedByUserId,
            AssignedUserId = workItem.AssignedUserId,
            CreatedAt = workItem.CreatedAt,
            UpdatedAt = workItem.UpdatedAt
        };
    }
}