using System.Collections.Concurrent;
using System.Security.Cryptography;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var log = app.Configuration.GetValue("Log", true);
var stats = new Stats();

app.MapPost("/hook", async (HttpContext context) =>
{
    var request = context.Request;
    var query = request.Query;
    Interlocked.Increment(ref stats.Received);

    if (DateTime.UtcNow.Ticks < Interlocked.Read(ref stats.DownUntilTicks))
    {
        Interlocked.Increment(ref stats.Dropped);
        Write(log, "down", request, 0, false);
        context.Abort();
        return;
    }

    using var buffer = new MemoryStream();
    await request.Body.CopyToAsync(buffer, context.RequestAborted);
    var hash = SHA256.HashData(buffer.GetBuffer().AsSpan(0, (int)buffer.Length));
    var bodyKey = BitConverter.ToUInt64(hash, 0);

    var wait = Number(query, "delay") + Random.Shared.Next(Number(query, "jitter") + 1);
    if (wait > 0)
    {
        try
        {
            await Task.Delay(wait, context.RequestAborted);
        }
        catch (OperationCanceledException)
        {
            Interlocked.Increment(ref stats.GaveUp);
            Write(log, "gave-up", request, buffer.Length, false);
            return;
        }
    }

    if (Roll(Number(query, "drop")))
    {
        Interlocked.Increment(ref stats.Dropped);
        Write(log, "dropped", request, buffer.Length, false);
        context.Abort();
        return;
    }

    var fail = Number(query, "fail");
    var status = Number(query, "status");
    var code = 200;
    if (fail > 0)
    {
        if (Roll(fail))
        {
            code = status != 0 ? status : 503;
        }
    }
    else if (status != 0)
    {
        code = status;
    }

    var retryAfter = Number(query, "retryAfter");
    if (code >= 300 && retryAfter > 0)
    {
        context.Response.Headers.RetryAfter = retryAfter.ToString();
    }

    var duplicate = false;
    if (code >= 200 && code < 300)
    {
        duplicate = stats.Delivered.AddOrUpdate(bodyKey, 1, (_, count) => count + 1) > 1;
        if (duplicate)
        {
            Interlocked.Increment(ref stats.Duplicates);
        }
    }

    stats.ByStatus.AddOrUpdate(code, 1, (_, count) => count + 1);
    Write(log, code.ToString(), request, buffer.Length, duplicate);
    context.Response.StatusCode = code;
});

app.MapGet("/stats", () => new
{
    received = Interlocked.Read(ref stats.Received),
    delivered = stats.Delivered.Count,
    duplicates = Interlocked.Read(ref stats.Duplicates),
    dropped = Interlocked.Read(ref stats.Dropped),
    gaveUp = Interlocked.Read(ref stats.GaveUp),
    byStatus = stats.ByStatus.OrderBy(p => p.Key).ToDictionary(p => p.Key.ToString(), p => p.Value),
});

app.MapPost("/down", (int seconds) =>
{
    Interlocked.Exchange(ref stats.DownUntilTicks, DateTime.UtcNow.AddSeconds(seconds).Ticks);
    return seconds > 0 ? $"down for {seconds} s" : "up";
});

app.MapPost("/reset", () =>
{
    stats = new Stats();
    return "reset";
});

app.Run();

static int Number(IQueryCollection query, string name) =>
    int.TryParse(query[name], out var value) ? value : 0;

static bool Roll(int percent) => percent > 0 && Random.Shared.Next(100) < percent;

static void Write(bool log, string outcome, HttpRequest request, long bytes, bool duplicate)
{
    if (!log)
    {
        return;
    }

    var headers = string.Join(" ", request.Headers
        .Where(h => h.Key.StartsWith("webhook-", StringComparison.OrdinalIgnoreCase)
                 || h.Key.StartsWith("x-", StringComparison.OrdinalIgnoreCase))
        .Select(h => $"{h.Key}={h.Value}"));
    Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} {outcome,-7} {request.Path}{request.QueryString} {bytes}B{(duplicate ? " DUPLICATE" : "")} {headers}");
}

sealed class Stats
{
    public long Received;
    public long Dropped;
    public long GaveUp;
    public long Duplicates;
    public long DownUntilTicks;
    public readonly ConcurrentDictionary<int, long> ByStatus = new();
    public readonly ConcurrentDictionary<ulong, int> Delivered = new();
}
