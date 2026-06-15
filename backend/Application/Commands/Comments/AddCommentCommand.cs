public class AddCommentCommand : ICommand<CommentResponse>
{
    private readonly Func<Task<CommentResponse>> _action;

    public AddCommentCommand(Func<Task<CommentResponse>> action)
    {
        _action = action;
    }

    public async Task<CommentResponse> ExecuteAsync()
    {
        return await _action();
    }
}