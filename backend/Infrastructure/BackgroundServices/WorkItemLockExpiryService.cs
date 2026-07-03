using backend.Application.Interfaces;

namespace backend.Infrastructure.BackgroundServices;

public class WorkItemLockExpiryService : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan InterestTtl = TimeSpan.FromMinutes(5);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<WorkItemLockExpiryService> _logger;

    public WorkItemLockExpiryService(
        IServiceScopeFactory scopeFactory,
        ILogger<WorkItemLockExpiryService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(CheckInterval, stoppingToken);
            try
            {
                await ProcessExpiredLocksAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in WorkItemLockExpiryService.");
            }
        }
    }

    private async Task ProcessExpiredLocksAsync()
    {
        await using var scope = _scopeFactory.CreateAsyncScope();

        var lockRepo = scope.ServiceProvider.GetRequiredService<IWorkItemLockRepository>();
        var interestRepo = scope.ServiceProvider.GetRequiredService<IWorkItemLockInterestRepository>();
        var expiryService = scope.ServiceProvider.GetRequiredService<IWorkItemLockExpiryService>();

        await interestRepo.RemoveStaleAsync(InterestTtl);

        var expiredLocks = await lockRepo.GetExpiredAsync();

        foreach (var expiredLock in expiredLocks)
        {
            try
            {
                await lockRepo.Delete(expiredLock);
                await expiryService.PromoteNextInterestedAsync(expiredLock.WorkItemId);

                _logger.LogInformation(
                    "Expired work item lock released for WorkItem {WorkItemId}.", expiredLock.WorkItemId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to process expired lock for WorkItem {WorkItemId}.", expiredLock.WorkItemId);
            }
        }
    }
}