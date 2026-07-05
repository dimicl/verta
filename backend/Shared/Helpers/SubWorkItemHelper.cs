public static class SubWorkItemHelper
{
    public static SubWorkItemResponse ToResponse(SubWorkItem subWorkItem)
    {
        return new SubWorkItemResponse
        {
            Id = subWorkItem.Id,
            Name = subWorkItem.Name,
            Description = subWorkItem.Description,
            Status = subWorkItem.Status,
            Priority = subWorkItem.Priority,
            WorkItemId = subWorkItem.WorkItemId,
            UserId = subWorkItem.UserId,
            AssignedUserId = subWorkItem.AssignedUserId,
            CreatedAt = subWorkItem.CreatedAt,
            UpdatedAt = subWorkItem.UpdatedAt
        };
    }
}