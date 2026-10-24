#!/bin/bash
# usage: TID=<tenant id> bench/sweep.sh
# Runs the API with 1, 2, 4 and 8 cooks; for each: warm-up, 2 x 30 s, average batch size from pg_stat_statements.
# Build Release first and make sure no other backend process is running.
B="$(cd "$(dirname "$0")" && pwd)"; OUT="$B/results"; mkdir -p "$OUT"
: "${TID:?set TID to the seeded tenant id}"
PSQL="${PSQL:-/c/Program Files/PostgreSQL/18/bin/psql.exe}"
q(){ PGPASSWORD="${PGPASSWORD:-admin}" "$PSQL" -h localhost -p 5433 -U postgres -d dispatchforge -tAc "$1"; }
cd "$B/../backend/backend"
for N in 1 2 4 8; do
  env "Ingest__CookCount=$N" "Logging__LogLevel__Default=Warning" "Logging__LogLevel__Microsoft.EntityFrameworkCore=Warning" dotnet run -c Release --no-build --launch-profile http > "$OUT/api-$N.log" 2>&1 &
  for i in $(seq 1 60); do curl -s -o /dev/null http://localhost:5066/ && break; sleep 1; done
  echo "=== cooks=$N"
  "$B/measure.sh" w$N 50 10s >/dev/null
  q "select pg_stat_statements_reset()" >/dev/null
  "$B/measure.sh" c$N-a 50 30s
  "$B/measure.sh" c$N-b 50 30s
  echo "batch: $(q "select round(sum(rows)::numeric/sum(calls),1) || ' events/batch, ' || round(sum(mean_exec_time*calls)::numeric/sum(calls),2) || ' ms/batch exec' from pg_stat_statements where query like 'INSERT INTO \"DeliveryOutboxes\"%'")"
  echo "errors in log: $(grep -c 'fail:' "$OUT/api-$N.log")"
  powershell -NoProfile -c "Stop-Process -Name backend" ; sleep 3
done
