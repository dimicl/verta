public class AddWorkItemFileCommand : ICommand<WorkItemFileResponse>
{
    private readonly Func<Task<WorkItemFileResponse>> _action;

    public AddWorkItemFileCommand(Func<Task<WorkItemFileResponse>> action)
    {
        _action = action;
    }

    public async Task<WorkItemFileResponse> ExecuteAsync()
    {
        return await _action();
    }
}