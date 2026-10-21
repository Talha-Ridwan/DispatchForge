using backend.DataStructure;
using backend.Entities;
using backend.Services;
using Npgsql;
using NpgsqlTypes;

namespace backend.BackgroundJob;

public class IngestWriterService : BackgroundService
{
    private const int MaxBatchSize = 500;
    private readonly IngestQueue _ingestQueue;
    private readonly NpgsqlDataSource _dataSource;
    private readonly ILogger<IngestWriterService> _logger;
    private readonly int _cookCount;

    public IngestWriterService(IngestQueue ingestQueue, NpgsqlDataSource dataSource, ILogger<IngestWriterService> logger, IConfiguration configuration)
    {
        _ingestQueue = ingestQueue;
        _dataSource = dataSource;
        _logger = logger;
        _cookCount = configuration.GetValue("Ingest:CookCount", 2);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var cooks = new Task[_cookCount];
        for (int i = 0; i < _cookCount; i++)
        {
            cooks[i] = CookLoopAsync(stoppingToken);
        }
        return Task.WhenAll(cooks);
    }

    private async Task CookLoopAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();
        var batch = new List<IngestTicket>(MaxBatchSize);

        while (await _ingestQueue.Reader.WaitToReadAsync(stoppingToken))
        {
            batch.Clear();
            while (batch.Count < MaxBatchSize && _ingestQueue.Reader.TryRead(out var ticket))
            {
                batch.Add(ticket);
            }

            IngestOutcome[] outcomes;
            try
            {
                outcomes = await WriteBatchAsync(batch, CancellationToken.None);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Ingest batch of {Count} failed", batch.Count);
                for (int i = 0; i < batch.Count; i++)
                {
                    batch[i].TaskCompletionSource.SetException(e);
                }
                continue;
            }

            for (int i = 0; i < batch.Count; i++)
            {
                batch[i].TaskCompletionSource.SetResult(outcomes[i]);
            }
        }
    }

    private async Task<IngestOutcome[]> WriteBatchAsync(List<IngestTicket> batch, CancellationToken ct)
    {
        batch.Sort(CompareByKey);

        await using var connection = await _dataSource.OpenConnectionAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);

        var inserted = await InsertIdempotencyRecordsAsync(connection, transaction, batch, ct);

        var outcomes = new IngestOutcome[batch.Count];
        var winners = new List<IngestTicket>(batch.Count);
        for (int i = 0; i < batch.Count; i++)
        {
            var record = batch[i].IdempotencyRecord;
            if (inserted.Remove((record.TenantId, Convert.ToBase64String(record.IdempotencyHash))))
            {
                outcomes[i] = IngestOutcome.Accepted;
                winners.Add(batch[i]);
            }
            else
            {
                outcomes[i] = IngestOutcome.Duplicate;
            }
        }

        if (winners.Count > 0)
        {
            await InsertEventsAsync(connection, transaction, winners, ct);
            await InsertOutboxesAsync(connection, transaction, winners, ct);
        }

        await transaction.CommitAsync(ct);
        return outcomes;
    }

    private static int CompareByKey(IngestTicket a, IngestTicket b)
    {
        var byTenant = a.IdempotencyRecord.TenantId.CompareTo(b.IdempotencyRecord.TenantId);
        if (byTenant != 0)
        {
            return byTenant;
        }

        return a.IdempotencyRecord.IdempotencyHash.AsSpan().SequenceCompareTo(b.IdempotencyRecord.IdempotencyHash);
    }

    private static async Task<HashSet<(Guid, string)>> InsertIdempotencyRecordsAsync(
        NpgsqlConnection connection, NpgsqlTransaction transaction, List<IngestTicket> batch, CancellationToken ct)
    {
        var tenantIds = new Guid[batch.Count];
        var hashes = new byte[batch.Count][];
        var createdAts = new DateTime[batch.Count];
        for (int i = 0; i < batch.Count; i++)
        {
            var record = batch[i].IdempotencyRecord;
            tenantIds[i] = record.TenantId;
            hashes[i] = record.IdempotencyHash;
            createdAts[i] = record.CreatedAt;
        }

        await using var command = new NpgsqlCommand(
            """
            INSERT INTO "IdempotencyRecords" ("TenantId", "IdempotencyHash", "CreatedAt")
            SELECT * FROM unnest(@tenantIds, @hashes, @createdAts)
            ON CONFLICT DO NOTHING
            RETURNING "TenantId", "IdempotencyHash"
            """, connection, transaction);
        command.Parameters.Add(new NpgsqlParameter<Guid[]>("tenantIds", tenantIds));
        command.Parameters.Add(new NpgsqlParameter<byte[][]>("hashes", hashes) { NpgsqlDbType = NpgsqlDbType.Array | NpgsqlDbType.Bytea });
        command.Parameters.Add(new NpgsqlParameter<DateTime[]>("createdAts", createdAts));

        var inserted = new HashSet<(Guid, string)>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            inserted.Add((reader.GetGuid(0), Convert.ToBase64String(reader.GetFieldValue<byte[]>(1))));
        }

        return inserted;
    }

    private static async Task InsertEventsAsync(
        NpgsqlConnection connection, NpgsqlTransaction transaction, List<IngestTicket> winners, CancellationToken ct)
    {
        var ids = new Guid[winners.Count];
        var receivedAts = new DateTime[winners.Count];
        var tenantIds = new Guid[winners.Count];
        var eventTypeIds = new Guid[winners.Count];
        var keys = new string[winners.Count];
        var payloads = new byte[winners.Count][];
        for (int i = 0; i < winners.Count; i++)
        {
            Event e = winners[i].Event;
            ids[i] = e.Id;
            receivedAts[i] = e.ReceivedAt;
            tenantIds[i] = e.TenantId;
            eventTypeIds[i] = e.EventTypeId;
            keys[i] = e.IdempotencyKey;
            payloads[i] = e.Payload;
        }

        await using var command = new NpgsqlCommand(
            """
            INSERT INTO "Events" ("Id", "ReceivedAt", "TenantId", "EventTypeId", "IdempotencyKey", "Payload")
            SELECT * FROM unnest(@ids, @receivedAts, @tenantIds, @eventTypeIds, @keys, @payloads)
            """, connection, transaction);
        command.Parameters.Add(new NpgsqlParameter<Guid[]>("ids", ids));
        command.Parameters.Add(new NpgsqlParameter<DateTime[]>("receivedAts", receivedAts));
        command.Parameters.Add(new NpgsqlParameter<Guid[]>("tenantIds", tenantIds));
        command.Parameters.Add(new NpgsqlParameter<Guid[]>("eventTypeIds", eventTypeIds));
        command.Parameters.Add(new NpgsqlParameter<string[]>("keys", keys));
        command.Parameters.Add(new NpgsqlParameter<byte[][]>("payloads", payloads) { NpgsqlDbType = NpgsqlDbType.Array | NpgsqlDbType.Bytea });

        await command.ExecuteNonQueryAsync(ct);
    }

    private static async Task InsertOutboxesAsync(
        NpgsqlConnection connection, NpgsqlTransaction transaction, List<IngestTicket> winners, CancellationToken ct)
    {
        var rows = new List<DeliveryOutbox>(winners.Count);
        for (int i = 0; i < winners.Count; i++)
        {
            rows.AddRange(winners[i].DeliveryOutboxes);
        }

        var eventIds = new Guid[rows.Count];
        var eventReceivedAts = new DateTime[rows.Count];
        var destinationIds = new Guid[rows.Count];
        var tenantIds = new Guid[rows.Count];
        var scheduledAts = new DateTime[rows.Count];
        for (int i = 0; i < rows.Count; i++)
        {
            eventIds[i] = rows[i].EventId;
            eventReceivedAts[i] = rows[i].EventReceivedAt;
            destinationIds[i] = rows[i].DestinationId;
            tenantIds[i] = rows[i].TenantId;
            scheduledAts[i] = rows[i].ScheduledAt;
        }

        await using var command = new NpgsqlCommand(
            """
            INSERT INTO "DeliveryOutboxes" ("EventId", "EventReceivedAt", "DestinationId", "TenantId", "ScheduledAt", "AttemptCount")
            SELECT *, 0 FROM unnest(@eventIds, @eventReceivedAts, @destinationIds, @tenantIds, @scheduledAts)
            """, connection, transaction);
        command.Parameters.Add(new NpgsqlParameter<Guid[]>("eventIds", eventIds));
        command.Parameters.Add(new NpgsqlParameter<DateTime[]>("eventReceivedAts", eventReceivedAts));
        command.Parameters.Add(new NpgsqlParameter<Guid[]>("destinationIds", destinationIds));
        command.Parameters.Add(new NpgsqlParameter<Guid[]>("tenantIds", tenantIds));
        command.Parameters.Add(new NpgsqlParameter<DateTime[]>("scheduledAts", scheduledAts));

        await command.ExecuteNonQueryAsync(ct);
    }
}
