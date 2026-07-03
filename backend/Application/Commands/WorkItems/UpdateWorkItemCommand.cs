public class UpdateWorkItemCommand : ICommand<WorkItemResponse>
{
    private readonly Func<Task<WorkItemResponse>> _action;

    public UpdateWorkItemCommand(Func<Task<WorkItemResponse>> action)
    {
        _action = action;
    }

    public Task<WorkItemResponse> ExecuteAsync() => _action();
}
