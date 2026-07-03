public class UpdateCommentCommand : ICommand<CommentResponse>
{
    private readonly Func<Task<CommentResponse>> _action;

    public UpdateCommentCommand(Func<Task<CommentResponse>> action)
    {
        _action = action;
    }

    public Task<CommentResponse> ExecuteAsync() => _action();
}
