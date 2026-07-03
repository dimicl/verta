public static class SprintHelper
{
    public static SprintResponse ToResponse(Sprint sprint)
    {
        return new SprintResponse
        {
            Id = sprint.Id,
            Name = sprint.Name,
            BoardId = sprint.BoardId,
            StartDate = sprint.StartDate,
            EndDate = sprint.EndDate,
            CreatedAt = sprint.CreatedAt,
            UpdatedAt = sprint.UpdatedAt
        };
    }
}
