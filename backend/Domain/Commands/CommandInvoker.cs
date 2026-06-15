public class CommandInvoker
{
    public async Task<TResult> ExecuteAsync<TResult>(ICommand<TResult> command)
    {
        return await command.ExecuteAsync();
    }
}