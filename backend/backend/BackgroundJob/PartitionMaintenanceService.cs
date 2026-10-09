using backend.Repositories;

namespace backend.BackgroundJob;

/*
 * Keeps the weekly Events partitions created ahead of time, and drops weeks past retention.
 * An insert with no matching partition FAILS, so if creating breaks, ingest breaks
 * (once the buffer runs out). That's why it's separate from the janitor and logs Critical.
 * Dropping failing is harmless short term (old data sticks around), so that logs Error.
 */
public class PartitionMaintenanceService : BackgroundService
{
    // This week + 3 ahead: a failing service gives us about 3 weeks to notice before ingest breaks
    private const int WeeksAhead = 3;

    // Every event lives at least this long, even one that arrived Sunday 23:59.
    // Must stay longer than the outbox age cap (3 days), or the FK safety net blocks drops.
    private static readonly TimeSpan Retention = TimeSpan.FromDays(7);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PartitionMaintenanceService> _logger;

    public PartitionMaintenanceService(IServiceScopeFactory scopeFactory, ILogger<PartitionMaintenanceService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Run once right away: PeriodicTimer waits a full interval before its first tick,
        // and a fresh database has no partitions at all
        await RunSafelyAsync(stoppingToken);

        using PeriodicTimer timer = new PeriodicTimer(TimeSpan.FromHours(12));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunSafelyAsync(stoppingToken);
        }
    }

    private async Task RunSafelyAsync(CancellationToken stoppingToken)
    {
        // Catch here so the exception never escapes ExecuteAsync; that would stop the whole app.
        // The `when` filter lets a normal shutdown (cancellation) pass through without a Critical log.
        try
        {
            await EnsurePartitionsAsync(stoppingToken);
        }
        catch (Exception e) when (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogCritical(e, "Partition maintenance failed; ingest breaks when the partition buffer runs out");
        }

        // Separate try: a failed drop must never stop partitions from being created
        try
        {
            await DropExpiredPartitionsAsync(stoppingToken);
        }
        catch (Exception e) when (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogError(e, "Listing partitions for retention failed, retrying next tick");
        }
    }

    private async Task EnsurePartitionsAsync(CancellationToken stoppingToken)
    {
        using IServiceScope scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IPartitionRepository>();

        DateTime monday = MondayOf(DateTime.UtcNow);

        for (int i = 0; i <= WeeksAhead; i++)
        {
            await repository.EnsureWeekPartitionAsync(monday.AddDays(7 * i), stoppingToken);
        }

        _logger.LogInformation("Partitions ensured from week of {Monday:yyyy-MM-dd} plus {WeeksAhead} ahead", monday, WeeksAhead);
    }

    private async Task DropExpiredPartitionsAsync(CancellationToken stoppingToken)
    {
        using IServiceScope scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IPartitionRepository>();

        List<DateTime> mondays = await repository.GetPartitionMondaysAsync(stoppingToken);
        DateTime now = DateTime.UtcNow;

        for (int i = 0; i < mondays.Count; i++)
        {
            DateTime monday = mondays[i];
            DateTime weekEnd = monday.AddDays(7);

            // The week's LAST possible event must be at least Retention old
            if (weekEnd + Retention > now)
            {
                continue;
            }

            // Per-week try: one blocked week (outbox rows still pointing in) must not stop the rest
            try
            {
                await repository.DropWeekPartitionAsync(monday, stoppingToken);
                _logger.LogInformation("Dropped Events partition for week of {Monday:yyyy-MM-dd}", monday);
            }
            catch (Exception e) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(e, "Could not drop Events partition for week of {Monday:yyyy-MM-dd}, retrying next tick", monday);
            }
        }
    }

    // DayOfWeek counts Sunday = 0 ... Saturday = 6. (day + 6) % 7 gives days since Monday: Mon 0, Sun 6
    private static DateTime MondayOf(DateTime utcNow)
    {
        int daysSinceMonday = ((int)utcNow.DayOfWeek + 6) % 7;
        return utcNow.Date.AddDays(-daysSinceMonday);
    }
}
