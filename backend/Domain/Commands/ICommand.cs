public interface ICommand<TResult>
{
    Task<TResult> ExecuteAsync();
}