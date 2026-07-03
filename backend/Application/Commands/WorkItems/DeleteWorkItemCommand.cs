public class DeleteWorkItemCommand : ICommand<bool>
{
    private readonly Func<Task<bool>> _action;

    public DeleteWorkItemCommand(Func<Task<bool>> action)
    {
        _action = action;
    }

    public Task<bool> ExecuteAsync() => _action();
}
