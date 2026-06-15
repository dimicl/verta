namespace backend.Application.Interfaces;

public interface ICommentService
{
    Task<CommentResponse> Create(CommentRequest request);
    Task<List<CommentResponse>> GetByWorkItemId(int workItemId);
}