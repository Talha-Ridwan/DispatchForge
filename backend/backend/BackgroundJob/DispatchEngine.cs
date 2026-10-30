using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using backend.DataStructure;
using Npgsql;

namespace backend.BackgroundJob;

public class DispatchEngine : BackgroundService
{
    private const int _maxDispatchEngines = 2;
    private const int _maxParallelSends = 50;
    private readonly NpgsqlDataSource _dataSource;
    private readonly ILogger<DispatchEngine> _logger;
    private readonly HttpClient _httpClient;
    private readonly int dispatchBuffer = 200;

    public DispatchEngine(
        NpgsqlDataSource dataSource,
        ILogger<DispatchEngine> logger,
        HttpClient httpClient)
    {
        _dataSource = dataSource;
        _logger = logger;
        _httpClient = httpClient;
    }


    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var dispatchers = new Task[_maxDispatchEngines];

        for (int i = 0; i < _maxDispatchEngines; i++)
        {
            var leasedBy = $"{Environment.MachineName}:{Environment.ProcessId}:{i}";
            dispatchers[i] = DispatchEngineAsync(leasedBy, stoppingToken);
        }

        return Task.WhenAll(dispatchers);
    }

    private async Task DispatchEngineAsync(string leasedBy, CancellationToken stoppingToken)
    {
        await Task.Yield();
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {

                var batch = await ClaimBatchAsync(leasedBy, stoppingToken);

                if (batch.Count == 0)
                {
                    await Task.Delay(500, stoppingToken);
                }
                await SendBatchAsync(batch, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {

            }
            catch (Exception e)
            {
                _logger.LogError(e, "Dispatcher failed");
                await Task.Delay(5000, stoppingToken);
            }
        }
    }

    private async Task<List<ClaimedDelivery>> ClaimBatchAsync(string leasedBy, CancellationToken stoppingToken)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(stoppingToken);
        await using var transaction = await connection.BeginTransactionAsync(stoppingToken);
        var queryToGet = $"""
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
                FOR UPDATE OF o SKIP LOCKED
                """;

        await using var getCommand = new NpgsqlCommand(queryToGet, connection, transaction);
        await using var reader = await getCommand.ExecuteReaderAsync(stoppingToken);

        var batch = new List<ClaimedDelivery>(dispatchBuffer);
        while (await reader.ReadAsync(stoppingToken))
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

        await reader.CloseAsync();
        var ids = new List<long>();

        for (int i = 0; i < batch.Count; i++)
        {
            ids.Add(batch[i].Id);
        }

        if(batch.Count == 0)
        {
            return batch;
        }

        var queryToUpdate = $"""
            UPDATE "DeliveryOutboxes"
            SET "LeasedBy" = @leasedBy,
            "LeaseExpiresAt" = NOW() + INTERVAL '120 seconds',
            "AttemptCount" = "AttemptCount" + 1,
            "ScheduledAt" = NOW()
                + LEAST(power(2, LEAST("AttemptCount", 10)) * INTERVAL '5 minutes', INTERVAL '6 hours')
                + random() * INTERVAL '5 minutes'
            WHERE "Id" = ANY(@ids)
            """;

        await using var updateCommand = new NpgsqlCommand(queryToUpdate, connection, transaction);
        updateCommand.Parameters.AddWithValue("leasedBy", leasedBy);
        updateCommand.Parameters.AddWithValue("ids", ids);
        await updateCommand.ExecuteNonQueryAsync(stoppingToken);
        await transaction.CommitAsync(stoppingToken);

        return batch;
    }

    public async Task SendBatchAsync(List<ClaimedDelivery> batch, CancellationToken stoppingToken)
    {
        var outcomes = new DeliveryOutcome[batch.Count];
        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = _maxParallelSends,
            CancellationToken = stoppingToken
        };

        try
        {
            await Parallel.ForAsync(0, batch.Count, options, async (i, _) =>
            {
                outcomes[i] = await SendOneAsync(batch[i], stoppingToken);
            });
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {

        }

        var deliveredIds = new List<long>();
        var unsentIds = new List<long>();
        for (int i = 0; i < batch.Count; i++)
        {
            if (outcomes[i] == DeliveryOutcome.Delivered)
            {
                deliveredIds.Add(batch[i].Id);
            }
            else if (outcomes[i] == DeliveryOutcome.NotStarted)
            {
                unsentIds.Add(batch[i].Id);
            }
        }

        await DeleteSentBatchAsync(deliveredIds);
        await RefundUnsentAsync(unsentIds);
    }

    private async Task<DeliveryOutcome> SendOneAsync(ClaimedDelivery delivery, CancellationToken stoppingToken)
    {
        try
        {
            using var timerToken = new CancellationTokenSource(delivery.TimeoutMilliseconds);
            using var content = new ByteArrayContent(delivery.Payload);
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
            content.Headers.Add("X-DispatchForge-Timestamp", timestamp);
            content.Headers.Add("X-DispatchForge-Signature", Sign(delivery.SigningKey, timestamp, delivery.Payload));
            using var linkedToken = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, timerToken.Token);
            using var response = await _httpClient.PostAsync(delivery.Url, content, linkedToken.Token);

            _logger.LogInformation("Row {Id} -> {Status}", delivery.Id, response.StatusCode);

            return response.IsSuccessStatusCode ? DeliveryOutcome.Delivered : DeliveryOutcome.Failed;
        }
        catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Timed out when delivering, outbox ID: {id}", delivery.Id);
            return DeliveryOutcome.Failed;
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            return DeliveryOutcome.NotStarted;
        }
        catch (HttpRequestException e)
        {
            _logger.LogWarning(e, "HTTP request exception on outbox ID : {id}", delivery.Id);
            return DeliveryOutcome.Failed;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Unexpected error on outbox ID : {id}", delivery.Id);
            return DeliveryOutcome.Failed;
        }
    }

    private static string Sign(byte[] key, string timestamp, byte[] payload)
    {
        var prefix = Encoding.UTF8.GetBytes(timestamp + ".");
        var toSign = new byte[prefix.Length + payload.Length];
        prefix.CopyTo(toSign, 0);
        payload.CopyTo(toSign, prefix.Length);
        return Convert.ToHexStringLower(HMACSHA256.HashData(key, toSign));
    }

    private async Task RefundUnsentAsync(List<long> ids)
    {
        if (ids.Count == 0)
        {
            return;
        }

        var queryToRefund = """
            UPDATE "DeliveryOutboxes"
            SET "AttemptCount" = "AttemptCount" - 1,
            "ScheduledAt" = NOW(),
            "LeasedBy" = NULL,
            "LeaseExpiresAt" = NULL
            WHERE "Id" = ANY(@ids)
            """;

        try
        {
            await using var connection = await _dataSource.OpenConnectionAsync(CancellationToken.None);
            await using var command = new NpgsqlCommand(queryToRefund, connection);
            command.Parameters.AddWithValue("ids", ids);
            await command.ExecuteNonQueryAsync(CancellationToken.None);
            _logger.LogInformation("Shutdown: refunded {Count} unsent rows", ids.Count);
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Refund of {Count} unsent rows failed", ids.Count);
        }
    }

    public async Task DeleteSentBatchAsync(List<long> deliveredIds)
    {
        if(deliveredIds.Count == 0)
        {
            return;
        }
        await using var connection = await _dataSource.OpenConnectionAsync(CancellationToken.None);
        var queryToDelete = """DELETE FROM "DeliveryOutboxes" WHERE "Id" = ANY(@ids)""";
        await using var command = new NpgsqlCommand(queryToDelete, connection);
        command.Parameters.AddWithValue("ids", deliveredIds);
        try
        {
            await command.ExecuteNonQueryAsync(CancellationToken.None);
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Deletion failed");
        }

    }
}
