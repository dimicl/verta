public class CreateWorkItemCommand : ICommand<WorkItemResponse>
{
    private readonly Func<Task<WorkItemResponse>> _action;

    public CreateWorkItemCommand(Func<Task<WorkItemResponse>> action)
    {
        _action = action;
    }

    public async Task<WorkItemResponse> ExecuteAsync()
    {
        return await _action();
    }
}