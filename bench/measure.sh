#!/bin/bash
# usage: TID=<tenant id> bench/measure.sh <label> <vus> <dur> [script.js]
# Prints req/s, p95 and CPU ms per request for the app and native Postgres.
B="$(cd "$(dirname "$0")" && pwd)"; OUT="$B/results"; mkdir -p "$OUT"
: "${TID:?set TID to the seeded tenant id}"
SCRIPT="${4:-ingest.js}"
read A0 P0 < <(powershell -NoProfile -File "$B/cpu.ps1" | tr -d '\r')
k6 run --quiet -e TID=$TID -e RUN=$1-$RANDOM$RANDOM -e VUS=$2 -e DUR=$3 --summary-export "$OUT/$1.json" "$B/$SCRIPT" >/dev/null 2>&1
read A1 P1 < <(powershell -NoProfile -File "$B/cpu.ps1" | tr -d '\r')
python - "$OUT/$1.json" $A0 $A1 $P0 $P1 <<'PY'
import json,sys
d=json.load(open(sys.argv[1])); m=d['metrics']; n=m['http_reqs']['count']
a=(float(sys.argv[3])-float(sys.argv[2]))/n; p=(float(sys.argv[5])-float(sys.argv[4]))/n
print(f"reqs={n} rate={m['http_reqs']['rate']:.0f}/s p95={m['http_req_duration']['p(95)']:.1f}ms fails={m['checks']['fails']} app={a:.2f} pg={p:.2f} total={a+p:.2f} ms/req")
PY
