using backend.Application.Interfaces;

namespace backend.Infrastructure.BackgroundServices;

public class BoardLockExpiryService : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(10);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BoardLockExpiryService> _logger;

    public BoardLockExpiryService(
        IServiceScopeFactory scopeFactory,
        ILogger<BoardLockExpiryService> logger)
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
                _logger.LogError(ex, "Unexpected error in BoardLockExpiryService.");
            }
        }
    }

    private async Task ProcessExpiredLocksAsync()
    {
        await using var scope = _scopeFactory.CreateAsyncScope();

        var lockRepo = scope.ServiceProvider.GetRequiredService<IBoardLockRepository>();
        var boardLockService = scope.ServiceProvider.GetRequiredService<IBoardLockPromotionService>();

        var expiredLocks = await lockRepo.GetExpiredAsync();

        foreach (var expiredLock in expiredLocks)
        {
            try
            {
                await lockRepo.Delete(expiredLock);
                await boardLockService.PromoteNextInQueueAsync(expiredLock.BoardId);

                _logger.LogInformation(
                    "Expired lock released for board {BoardId}.", expiredLock.BoardId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to process expired lock for board {BoardId}.", expiredLock.BoardId);
            }
        }
    }
}