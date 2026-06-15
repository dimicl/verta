public class DeleteWorkItemFileCommand : ICommand<bool>
{
    private readonly Func<Task<bool>> _action;

    public DeleteWorkItemFileCommand(Func<Task<bool>> action)
    {
        _action = action;
    }

    public async Task<bool> ExecuteAsync()
    {
        return await _action();
    }
}