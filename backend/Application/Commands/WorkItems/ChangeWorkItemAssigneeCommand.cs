public class ChangeWorkItemAssigneeCommand : ICommand<WorkItemResponse>
{
    private readonly Func<Task<WorkItemResponse>> _action;

    public ChangeWorkItemAssigneeCommand(Func<Task<WorkItemResponse>> action)
    {
        _action = action;
    }

    public Task<WorkItemResponse> ExecuteAsync() => _action();
}
