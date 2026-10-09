using System.Globalization;
using backend.Data;
using Microsoft.EntityFrameworkCore;

namespace backend.Repositories;

public class PartitionRepository : IPartitionRepository
{
    private const string NamePrefix = "Events_";
    private const string NameDateFormat = "yyyyMMdd";

    private readonly AppDbContext _dbContext;
    public PartitionRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task EnsureWeekPartitionAsync(DateTime monday, CancellationToken cancellationToken)
    {
        DateTime nextMonday = monday.AddDays(7);
        string name = PartitionName(monday);
        string from = monday.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        string to = nextMonday.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        string sql = $"""
            CREATE TABLE IF NOT EXISTS "{name}"
                PARTITION OF "Events"
                FOR VALUES FROM ('{from} 00:00:00+00') TO ('{to} 00:00:00+00');
            """;

        await _dbContext.Database.ExecuteSqlRawAsync(sql, cancellationToken);
    }
    
    public async Task<List<DateTime>> GetPartitionMondaysAsync(CancellationToken cancellationToken)
    {
        List<string> names = await _dbContext.Database
            .SqlQueryRaw<string>("""
                SELECT c.relname AS "Value"
                FROM pg_inherits i
                JOIN pg_class c ON c.oid = i.inhrelid
                WHERE i.inhparent = '"Events"'::regclass
                """)
            .ToListAsync(cancellationToken);

        List<DateTime> mondays = new List<DateTime>();
        for (int i = 0; i < names.Count; i++)
        {
            string name = names[i];
            if (!name.StartsWith(NamePrefix))
            {
                continue;
            }

            if (DateTime.TryParseExact(
                    name.Substring(NamePrefix.Length),
                    NameDateFormat,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out DateTime monday))
            {
                mondays.Add(monday);
            }
        }

        return mondays;
    }

    /*
     * Removes one week of events. Two steps, one transaction:
     *  1. DETACH: unplug the partition from Events. This is where Postgres checks the outbox FK,
     *     so it THROWS if any outbox row still points into this week (the safety net).
     *  2. DROP: throw the now-standalone table away.
     * A plain DROP always fails (the FK depends on the partition). NEVER use DROP ... CASCADE:
     * it would silently delete the FK constraint itself, i.e. remove the safety net.
     * One transaction so a failed DROP can't leave a detached orphan table behind.
     */
    public async Task DropWeekPartitionAsync(DateTime monday, CancellationToken cancellationToken)
    {
        string name = PartitionName(monday);

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        await _dbContext.Database.ExecuteSqlRawAsync(
            $"""ALTER TABLE "Events" DETACH PARTITION "{name}";""", cancellationToken);
        await _dbContext.Database.ExecuteSqlRawAsync(
            $"""DROP TABLE "{name}";""", cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }

    private static string PartitionName(DateTime monday)
    {
        return NamePrefix + monday.ToString(NameDateFormat, CultureInfo.InvariantCulture);
    }
}
