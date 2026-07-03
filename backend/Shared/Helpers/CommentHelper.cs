public static class CommentHelper
{
    public static CommentResponse ToResponse(Comment comment)
    {
        return new CommentResponse
        {
            Id = comment.Id,
            Content = comment.Content,
            WorkItemId = comment.WorkItemId,
            UserId = comment.UserId,
            FirstName = comment.User?.FirstName ?? string.Empty,
            LastName = comment.User?.LastName ?? string.Empty,
            CreatedAt = comment.CreatedAt,
            UpdatedAt = comment.UpdatedAt
        };
    }
}