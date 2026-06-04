import { loadConfig, type CityConfig } from "./config.ts";
import { initClobClient } from "./clob.ts";
import { fetchNoaa } from "./fetchers/noaa.ts";
import { fetchAviationWeather } from "./fetchers/aviation-weather.ts";
import { handleObs } from "./hot-window.ts";
import { evaluateBuckets } from "./evaluator.ts";
import { postOrder } from "./trader.ts";
import { prepareOrders, getPreparedOrders } from "./order-cache.ts";
import { refreshBooks, startBookStream, getCachedAsksFast, applyMarketMessage } from "./book-cache.ts";
import type { BucketState, ObservationResult } from "./types.ts";
import { log } from "./logger.ts";
import { todaySlug } from "./time.ts";

interface GammaMarket {
  question: string;
  clobTokenIds: string;
  conditionId: string;
}

interface GammaEvent {
  negRisk: boolean;
  markets: GammaMarket[];
}

async function resolveMarkets(city: CityConfig): Promise<BucketState[]> {
  const slug = todaySlug(city.slug);
  const resp = await fetch(`https://gamma-api.polymarket.com/events?slug=${slug}`);
  const events = await resp.json() as GammaEvent[];
  const event = events[0]!;

  const buckets: BucketState[] = event.markets.map(m => {
    const [yesId, noId] = JSON.parse(m.clobTokenIds) as [string, string];
    const tempMatch = m.question.match(/(\d+)°C/);
    const tempC = tempMatch ? parseInt(tempMatch[1]!, 10) : 0;
    const lq = m.question.toLowerCase();
    const type: "exact" | "below" | "above" =
      lq.includes("or below") ? "below" :
      lq.includes("or higher") ? "above" : "exact";

    return {
      tempC,
      type,
      noTokenId: noId!,
      yesTokenId: yesId!,
      conditionId: m.conditionId,
      negRisk: event.negRisk,
      bought: false,
      attempted: false,
      pendingBuy: false,
      peakBought: false,
    };
  });

  buckets.sort((a, b) => a.tempC - b.tempC);
  return buckets;
}

function percentile(sorted: number[], p: number): number {
  const idx = Math.floor((sorted.length - 1) * p);
  return sorted[idx]!;
}

async function runBenchmark(): Promise<void> {
  const benchmarkStart = performance.now();
  const config = loadConfig();
  const clob = await initClobClient(config);

  const city = config.cities[0]!;
  const buckets = await resolveMarkets(city);
  const exactTokenIds = buckets.filter(b => b.type === "exact").map(b => b.noTokenId);

  startBookStream(exactTokenIds, city.icao);
  const prewarmPromise = prepareOrders(clob, buckets, config);
  await refreshBooks(clob, exactTokenIds);
  await prewarmPromise;
  await Bun.sleep(300);

  const initMs = performance.now() - benchmarkStart;
  const preparedHitRate = exactTokenIds.filter(id => getPreparedOrders(id)).length / exactTokenIds.length;

  const originalPostOrder = clob.postOrder.bind(clob);
  let submitCapturedAt = 0;
  clob.postOrder = (...args: any[]) => {
    submitCapturedAt = performance.now();
    return Promise.resolve({ status: "matched" }) as any;
  };

  // Suppress stdout I/O during the measurement loop to avoid overhead
  const originalStdoutWrite = process.stdout.write.bind(process.stdout);
  process.stdout.write = (() => true) as typeof process.stdout.write;



  const N = 5000;
  const detectionToSubmit: number[] = [];
  const evaluatorTimes: number[] = [];
  const traderFastpathTimes: number[] = [];

  for (let i = 0; i < N; i++) {
    const observedMaxRef = { value: -Infinity };
    handleObs({ tempC: 20, observedAtUtcMs: Date.now() }, "benchmark", city.icao, observedMaxRef, { value: false }, new Map(), buckets, clob, config);

    for (const b of buckets) {
      b.bought = false;
      b.attempted = false;
      b.pendingBuy = false;
    }
    observedMaxRef.value = -Infinity;

    const t0 = performance.now();
    observedMaxRef.value = 21;
    const detectedAt = performance.now();

    evaluateBuckets(21, buckets, b => {
      const tEval = performance.now();
      const prepared = getPreparedOrders(b.noTokenId);
      if (prepared) {
        const tPreCall = performance.now();
        clob.postOrder(prepared.limitOrder, "FAK" as any);
        traderFastpathTimes.push(submitCapturedAt - tPreCall);
      }
      evaluatorTimes.push(tEval - detectedAt);
    });

    detectionToSubmit.push(submitCapturedAt - t0);
  }

  clob.postOrder = originalPostOrder;
  process.stdout.write = originalStdoutWrite;

  // Fetcher latency (with 8s timeout each to tolerate slow network during benchmark)
  const fetchWithTimeout = <T>(fn: () => Promise<T>, ms: number): Promise<T | null> =>
    Promise.race([fn(), Bun.sleep(ms).then(() => null)]);

  const noaaStart = performance.now();
  const awStart = performance.now();
  const [noaaResult, awResult] = await Promise.all([
    fetchWithTimeout(() => fetchNoaa(city.icao), 8000),
    fetchWithTimeout(() => fetchAviationWeather(city.icao), 8000),
  ]);
  const noaaMs = noaaResult === null ? 8000 : performance.now() - noaaStart;
  const awMs = awResult === null ? 8000 : performance.now() - awStart;

  const dtsSorted = [...detectionToSubmit].sort((a, b) => a - b);
  const evalSorted = [...evaluatorTimes].sort((a, b) => a - b);
  const traderSorted = [...traderFastpathTimes].sort((a, b) => a - b);

  const dtsP50 = percentile(dtsSorted, 0.5);
  const evalP50 = percentile(evalSorted, 0.5);
  const traderP50 = percentile(traderSorted, 0.5);

  const bookCacheHitRate = exactTokenIds.filter(id => getCachedAsksFast(id) !== null).length / exactTokenIds.length;

  console.log(`METRIC detection_to_submit_p50_us=${Math.round(dtsP50 * 1000)}`);
  console.log(`METRIC evaluator_us=${Math.round(evalP50 * 1000)}`);
  console.log(`METRIC trader_fastpath_us=${Math.round(traderP50 * 1000)}`);
  console.log(`METRIC prepared_hit_rate=${preparedHitRate}`);
  console.log(`METRIC book_cache_hit_rate=${bookCacheHitRate}`);
  console.log(`METRIC init_ms=${Math.round(initMs * 10) / 10}`);
  console.log(`METRIC fetcher_noaa_ms=${Math.round(noaaMs * 10) / 10}`);
  console.log(`METRIC fetcher_aw_ms=${Math.round(awMs * 10) / 10}`);

  log("benchmark", `done detection_to_submit_p50=${Math.round(dtsP50 * 1000)}µs prepared=${preparedHitRate} book=${bookCacheHitRate}`);
}

await runBenchmark();
process.exit(0);
