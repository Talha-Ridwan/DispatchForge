using backend.Exceptions;
using backend.Repositories;

namespace backend.BackgroundJob;

public class CleanupService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CleanupService> _logger;

    public CleanupService(IServiceScopeFactory scopeFactory, ILogger<CleanupService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using PeriodicTimer timer = new PeriodicTimer(TimeSpan.FromMinutes(30));

        while(await timer.WaitForNextTickAsync(stoppingToken))
        {
            // Catch here so the exception never escapes ExecuteAsync; that would stop the whole app
            try
            {
                await CleanupEventTypesAsync();
            }
            catch (AyoJanitorChokedAndBouncedException e)
            {
                _logger.LogError(e, "Janitor crew bounced, retrying next tick");
            }
        }
    }

    private async Task CleanupEventTypesAsync()
    {
        try
        {
            using IServiceScope scope = _scopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IEventTypeRepository>();

            int count = await repository.DeleteMarkedEventTypesAsync();
            _logger.LogInformation("Janitor crew removed {Count} event types", count);
        }
        catch (Exception e)
        {
            throw new AyoJanitorChokedAndBouncedException("Cleanup service failed while deleting marked event types", e);
        }
    }
}
