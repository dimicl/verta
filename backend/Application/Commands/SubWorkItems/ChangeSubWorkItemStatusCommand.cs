public class ChangeSubWorkItemStatusCommand : ICommand<SubWorkItemResponse>
{
    private readonly Func<Task<SubWorkItemResponse>> _action;

    public ChangeSubWorkItemStatusCommand(Func<Task<SubWorkItemResponse>> action)
    {
        _action = action;
    }

    public async Task<SubWorkItemResponse> ExecuteAsync()
    {
        return await _action();
    }
}