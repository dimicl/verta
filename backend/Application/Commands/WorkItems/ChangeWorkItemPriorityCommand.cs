public class ChangeWorkItemPriorityCommand : ICommand<WorkItemResponse>
{
    private readonly Func<Task<WorkItemResponse>> _action;

    public ChangeWorkItemPriorityCommand(Func<Task<WorkItemResponse>> action)
    {
        _action = action;
    }

    public Task<WorkItemResponse> ExecuteAsync() => _action();
}
