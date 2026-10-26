# MockReceiver

A fake webhook subscriber for testing the dispatcher. Listens on `http://localhost:5077`.

Run it from `backend/MockReceiver`:

```
dotnet run                      # logs one line per request
dotnet run -- --Log=false       # quiet, for benchmarks
```

## Behavior per destination

Each destination's URL picks its own behavior through query parameters. All are optional;
plain `/hook` answers 200 immediately.

| Parameter | Meaning |
|---|---|
| `delay=ms` | wait this long before answering |
| `jitter=ms` | add a random 0..ms on top of `delay` |
| `status=code` | answer with this code (always, or only on failures when `fail` is set) |
| `fail=pct` | this percent of requests fail with `status` (default 503) |
| `drop=pct` | this percent of requests get the connection cut, no response |
| `retryAfter=s` | add a `Retry-After` header to failures |

Examples:

| URL | Simulates |
|---|---|
| `/hook` | a healthy, fast receiver (benchmarks) |
| `/hook?status=404` | a gone endpoint (dead-letter test) |
| `/hook?delay=25000` | a receiver slower than the 20 s timeout |
| `/hook?fail=30` | 30% 503s (retry test) |
| `/hook?status=429&retryAfter=10` | rate limiting |
| `/hook?delay=50&jitter=200&fail=10&drop=5` | a realistic flaky server |

## Offline

- `POST /down?seconds=60` cuts every `/hook` connection for 60 s, then recovers on its own. `seconds=0` brings it back now.
- To get a refused connection instead, point a destination at a port nothing listens on, like `http://localhost:5999/hook`.

## Stats

- `GET /stats`: received, delivered (unique bodies that got a 2xx), duplicates (a body that got a 2xx again), dropped, gaveUp (the sender hung up during a delay), byStatus.
- `POST /reset` clears them.

Duplicates are matched on the body bytes, so two different events with identical payloads count as one.
