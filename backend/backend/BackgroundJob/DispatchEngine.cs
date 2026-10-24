using System;
using Npgsql;

namespace backend.BackgroundJob;

public class DispatchEngine : BackgroundService
{
    private const int _maxDispatchEngines = 2;
    private readonly NpgsqlDataSource _dataSource;
    private readonly ILogger<DispatchEngine> _logger;
    private readonly int dispatchBuffer = 200;

    public DispatchEngine(
        NpgsqlDataSource dataSource,
        ILogger<DispatchEngine> logger)
    {
        _dataSource = dataSource;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var dispatchers = new Task[_maxDispatchEngines];

        for (int i = 0; i < _maxDispatchEngines; i++)
        {
            dispatchers[i] = DispatchEngineAsync(stoppingToken);
        }

        return Task.WhenAll(dispatchers);
    }

    private async Task DispatchEngineAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();
        
    }
}
