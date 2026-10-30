using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var log = app.Configuration.GetValue("Log", true);
var signingKey = Convert.FromHexString(app.Configuration.GetValue("SigningKeyHex", "")!);
var stats = new Stats();
Console.WriteLine(signingKey.Length > 0
    ? $"Verifying signatures with a {signingKey.Length}-byte key"
    : "Signature check off (no SigningKeyHex)");

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

    var signature = CheckSignature(signingKey, request, buffer.GetBuffer().AsSpan(0, (int)buffer.Length));
    if (signature != "ok" && signature != "off")
    {
        Interlocked.Increment(ref stats.BadSignatures);
        stats.ByStatus.AddOrUpdate(401, 1, (_, count) => count + 1);
        Write(log, $"sig-{signature}", request, buffer.Length, false);
        context.Response.StatusCode = 401;
        return;
    }

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
    badSignatures = Interlocked.Read(ref stats.BadSignatures),
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

// Mirrors what a real receiver does: recompute HMAC-SHA256 over "<timestamp>.<body>",
// compare in constant time, and refuse timestamps more than 5 minutes off.
static string CheckSignature(byte[] key, HttpRequest request, ReadOnlySpan<byte> body)
{
    if (key.Length == 0)
    {
        return "off";
    }

    var timestamp = request.Headers["X-DispatchForge-Timestamp"].ToString();
    var signature = request.Headers["X-DispatchForge-Signature"].ToString();
    if (!long.TryParse(timestamp, out var seconds) || signature.Length != 64)
    {
        return "missing";
    }

    if (Math.Abs(DateTimeOffset.UtcNow.ToUnixTimeSeconds() - seconds) > 300)
    {
        return "stale";
    }

    var prefix = Encoding.UTF8.GetBytes(timestamp + ".");
    var toSign = new byte[prefix.Length + body.Length];
    prefix.CopyTo(toSign, 0);
    body.CopyTo(toSign.AsSpan(prefix.Length));
    var expected = HMACSHA256.HashData(key, toSign);

    try
    {
        return CryptographicOperations.FixedTimeEquals(expected, Convert.FromHexString(signature)) ? "ok" : "bad";
    }
    catch (FormatException)
    {
        return "bad";
    }
}

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
    public long BadSignatures;
    public long DownUntilTicks;
    public readonly ConcurrentDictionary<int, long> ByStatus = new();
    public readonly ConcurrentDictionary<ulong, int> Delivered = new();
}
