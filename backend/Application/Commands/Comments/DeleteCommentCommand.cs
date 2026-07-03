public class DeleteCommentCommand : ICommand<bool>
{
    private readonly Func<Task<bool>> _action;

    public DeleteCommentCommand(Func<Task<bool>> action)
    {
        _action = action;
    }

    public Task<bool> ExecuteAsync() => _action();
}
