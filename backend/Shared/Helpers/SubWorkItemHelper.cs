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
            WorkItemId = subWorkItem.WorkItemId,
            UserId = subWorkItem.UserId,
            CreatedAt = subWorkItem.CreatedAt,
            UpdatedAt = subWorkItem.UpdatedAt
        };
    }
}