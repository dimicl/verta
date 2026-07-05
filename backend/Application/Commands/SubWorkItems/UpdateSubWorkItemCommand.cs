public class UpdateSubWorkItemCommand : ICommand<SubWorkItemResponse>
{
    private readonly Func<Task<SubWorkItemResponse>> _action;

    public UpdateSubWorkItemCommand(Func<Task<SubWorkItemResponse>> action)
    {
        _action = action;
    }

    public async Task<SubWorkItemResponse> ExecuteAsync()
    {
        return await _action();
    }
}
