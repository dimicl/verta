using Microsoft.Extensions.Logging;
public class CommandInvoker
{
    private readonly ILogger<CommandInvoker> _logger;

    public CommandInvoker(ILogger<CommandInvoker> logger)
    {
        _logger = logger;
    }

    public async Task<TResult> ExecuteAsync<TResult>(ICommand<TResult> command)
    {
        var commandName = command.GetType().Name;
        _logger.LogInformation("Executing command {CommandName}", commandName);

        try
        {
            var result = await command.ExecuteAsync();
            _logger.LogInformation("Completed command {CommandName}", commandName);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed command {CommandName}", commandName);
            throw;
        }
    }
}
