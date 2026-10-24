# Bench

Load tests for ingest (PDD §7). Native k6 (on PATH), native Postgres on port 5433, Git Bash.

| File | What it does |
|---|---|
| `ingest.js` | k6: every request has a unique idempotency key |
| `dups.js` | k6: keys drawn from 200 values per run, so most requests are duplicates (expect exactly 200 Events per run) |
| `cpu.ps1` | prints cumulative CPU ms of the API process and of all postgres processes |
| `measure.sh` | CPU snapshot, k6 run, CPU snapshot; prints req/s, p95, failed checks and CPU ms per request |
| `sweep.sh` | restarts the API with 1/2/4/8 cooks and runs `measure.sh` for each |

Results (k6 summaries, API logs) land in `bench/results/`, which is git-ignored.

## Run

1. Build and start the API in Release with quiet logs (from `backend/backend`):
   ```bash
   dotnet build -c Release
   env "Logging__LogLevel__Default=Warning" "Logging__LogLevel__Microsoft.EntityFrameworkCore=Warning" \
     dotnet run -c Release --no-build --launch-profile http
   ```
2. Seed once (needs an Admin JWT from `/api/User/register` + `/api/User/login`):
   `POST /api/Tenant {"Name"}`, then `POST /api/tenants/{tid}/event-types {"Name":"order.created"}`,
   then `POST /api/tenants/{tid}/destinations {"Url":"https://example.com/hook","EventTypes":["order.created"]}`.
3. Measure:
   ```bash
   TID=<tenant id> bench/measure.sh warmup 50 10s     # ignore this one
   TID=<tenant id> bench/measure.sh run-a 50 30s
   TID=<tenant id> bench/measure.sh dups 50 30s dups.js
   ```

## Rules

- Warm up first; compare CPU per request, not req/s; alternate A/B/A; only compare numbers from the same machine and setup.
- `Get-Counter` sometimes glitches when a postgres process starts or exits mid-run. Rerun when pg ms/req looks far off.
- Reset bench data with `TRUNCATE "DeliveryOutboxes", "IdempotencyRecords", "Events"` (keeps the seed).
