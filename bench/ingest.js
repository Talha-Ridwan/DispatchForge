import http from 'k6/http'; import { check } from 'k6'; import exec from 'k6/execution';
export const options = { vus: Number(__ENV.VUS || 50), duration: __ENV.DUR || '30s' };
const BASE = __ENV.BASE || 'http://localhost:5066';
export default function () {
  const key = `${__ENV.RUN}-${exec.vu.idInTest}-${exec.vu.iterationInScenario}`;
  const body = JSON.stringify({ eventType: 'order.created', idempotencyKey: key,
    payload: { orderId: key, amount: 4999, currency: 'USD', items: [{ sku: 'DUCK-01', qty: 2 }] } });
  const res = http.post(`${BASE}/api/tenants/${__ENV.TID}/events`, body, { headers: { 'Content-Type': 'application/json' } });
  check(res, { '202': (r) => r.status === 202 });
}
