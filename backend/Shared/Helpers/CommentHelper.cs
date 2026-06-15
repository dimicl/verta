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
            CreatedAt = comment.CreatedAt,
            UpdatedAt = comment.UpdatedAt
        };
    }
}