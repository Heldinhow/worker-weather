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
<!-- Updated during loop -->
