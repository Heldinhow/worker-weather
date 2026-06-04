# Autoresearch: Weather Bot — Detection-to-Trade Latency

## Objective
Minimize wall-clock time from `handleObs` setting `ref.value = obs.tempC` (new ObservedMax detected)
to `clob.postOrder(prepared.limitOrder, OrderType.FAK)` being called.

The hot path in production is:
`handleObs` → `evaluateBuckets` (sync scan) → `postOrder` callback → `getPreparedOrders` → `getCachedAsksFast` → `clob.postOrder(...)`

All of this is synchronous in-process work. Any latency here is avoidable overhead.

## Metrics
- **Primary**: `detection_to_submit_p50_us` (µs, lower is better) — median latency from detection to `clob.postOrder` call
- **Secondary**:
  - `evaluator_us`: time inside `evaluateBuckets`
  - `trader_fastpath_us`: time inside `postOrder` fast path
  - `prepared_hit_rate`: fraction of runs where prepared orders existed
  - `book_cache_hit_rate`: fraction of runs where book cache had data
  - `fetcher_noaa_ms`: NOAA fetch latency
  - `fetcher_aw_ms`: AviationWeather fetch latency

## How to Run
`./autoresearch.sh` — outputs `METRIC name=number` lines.

Runs `bun run src/benchmark.ts` which:
1. Inits real CLOB client (no dry run — real code paths)
2. Resolves one city's markets for today
3. Prepares orders (real `createOrder` / `createMarketOrder` calls)
4. Starts book WebSocket stream
5. Polls book via REST once
6. Waits 3s for WebSocket data
7. Runs 100 synthetic observations through `handleObs` → `evaluateBuckets` → `postOrder`
8. Monkey-patches `clob.postOrder` to capture timestamp without submitting real orders
9. Also does one parallel fetch from NOAA and AviationWeather
10. Reports median latencies

## Files in Scope
- `src/hot-window.ts` — `handleObs`, `runHotObservationLoops`
- `src/evaluator.ts` — `evaluateBuckets`
- `src/trader.ts` — `postOrder`
- `src/order-cache.ts` — `getPreparedOrders`, `prepareOrders`
- `src/book-cache.ts` — `getCachedAsksFast`, `refreshBooks`, `startBookStream`
- `src/fetchers/noaa.ts` — `fetchNoaa`
- `src/fetchers/aviation-weather.ts` — `fetchAviationWeather`
- `src/benchmark.ts` — benchmark harness (can be modified)
- `autoresearch.sh` — benchmark runner

## Off Limits
- Do not change order type from FAK
- Do not change LIMIT_PRICE (0.99)
- Do not remove blind experiment fallback
- Do not change Hot Window boundaries (53–04)
- Do not add retries on 400 "no orders found"

## Constraints
- TypeScript must type-check (`bunx tsc --noEmit`)
- No new runtime dependencies

## What's Been Tried
1. **Remove getCachedAsksFast from hot path** (`trader.ts`) — eliminated unnecessary Map.get and reason ternary from prepared fast path. Saved ~1µs.
2. **Non-blocking hot window entry** (`hot-window.ts`) — `refreshBooks` + `prepareOrders` now fire-and-forget instead of blocking first fetch. Removes ~400-600ms stall.
3. **Precise sleep scheduling** (`hot-window.ts`) — replaced fixed 30s sleep with `msUntilNextHotWindow`, waking exactly at window open. Eliminates up-to-29s oversleep.
4. **NOAA parsing optimization + connection warm-up** (`fetchers/noaa.ts`, `hot-window.ts`) — zero-allocation line parsing, warm-up fetch before window. NOAA warm fetch 4x faster (168ms vs 702ms).
5. **Reduced fetch timeout** — 12s → 5s for faster failure recovery.
6. **Periodic REST book refresh during hot window** — 10s (later 5s) interval as WS fallback.
7. **WS health check + force reconnect** — verifies WS is connected before window, reconnects immediately if not.
8. **WS max reconnect delay 30s → 5s** — faster recovery after WS drops.
9. **CLOB REST warm-up** — pre-connects TLS for `getOrderBook` before window.
10. **formatBrt cache** — avoids repeated Date/toISOString for same millisecond.
11. **exactBuckets on hot path** — `evaluateBuckets` only iterates exact buckets, skipping below/above.
12. **Set<AbortController>** — O(1) deletion instead of O(n) splice in fetch loop.
13. **await prepareOrders on init** — guarantees 100% prepared hit rate before window.
14. **Cache-busting on both fetchers** — `?_=${Date.now()}` avoids CDN staleness.
15. **keepalive: true** on both fetchers — ensures HTTP connection reuse.
16. **Logger tag color cache** — O(1) lookup instead of O(n) iteration.
17. **Sorted insertion in book cache** (`applyAskChange`, `normalizeOrders`) — avoids full-array sorts on every WS message.

## Deferred Ideas
- See `autoresearch.ideas.md`
