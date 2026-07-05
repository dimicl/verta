namespace backend.Application.Interfaces;

public interface ICommentService
{
    Task<CommentResponse> Create(CommentRequest request);
    Task<List<CommentResponse>> GetByWorkItemId(int workItemId);
    Task<List<CommentResponse>> GetBySubWorkItemId(int subWorkItemId);
    Task<CommentResponse> Update(int commentId, UpdateCommentRequest request);
    Task Delete(int commentId);
}