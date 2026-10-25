using System;
using backend.DataStructure;
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
        while(!stoppingToken.IsCancellationRequested)
        {
            var batch = await ClaimBatchAsync(stoppingToken);

            if(batch.Count == 0)
            {
                await Task.Delay(500, stoppingToken);
            }

            
        }
    }

    private async Task<List<ClaimedDelivery>> ClaimBatchAsync(CancellationToken stoppingToken)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(stoppingToken);
        var query = $"""
                SELECT o."Id", o."EventId", o."EventReceivedAt", o."DestinationId", o."AttemptCount",
                       e."Payload",
                       d."Url", d."CryptographyKeyPrimary", d."TimeoutMilliseconds"
                FROM "DeliveryOutboxes" o
                JOIN "Events" e       ON o."EventId" = e."Id" AND o."EventReceivedAt" = e."ReceivedAt"
                JOIN "Destinations" d ON o."DestinationId" = d."Id"
                WHERE (o."LeaseExpiresAt" <= NOW() OR o."LeaseExpiresAt" IS NULL)
                  AND o."ScheduledAt" <= NOW()
                ORDER BY o."ScheduledAt"
                LIMIT {dispatchBuffer}
                """;
            await using var command = new NpgsqlCommand(query, connection);
            await using var reader = await command.ExecuteReaderAsync(stoppingToken);

            var batch = new List<ClaimedDelivery>(dispatchBuffer);
            while(await reader.ReadAsync(stoppingToken))
            {
                batch.Add(new ClaimedDelivery(
                    reader.GetInt64(0),
                    reader.GetGuid(1),
                    reader.GetDateTime(2),
                    reader.GetGuid(3),
                    reader.GetInt32(4),
                    reader.GetFieldValue<byte[]>(5),
                    reader.GetString(6),
                    reader.GetFieldValue<byte[]>(7),
                    reader.GetInt32(8)));
            }

            return batch;
    }
}
