public class DeleteSubWorkItemCommand : ICommand<bool>
{
    private readonly Func<Task<bool>> _action;

    public DeleteSubWorkItemCommand(Func<Task<bool>> action)
    {
        _action = action;
    }

    public Task<bool> ExecuteAsync() => _action();
}
