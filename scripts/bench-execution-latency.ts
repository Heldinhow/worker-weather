import type { ClobClient } from "@polymarket/clob-client-v2";
import type { Config } from "../src/config.ts";
import type { BucketState, ObservationResult } from "../src/types.ts";
import { handleObs, runHotObservationLoops } from "../src/hot-window.ts";
import { prepareOrders } from "../src/order-cache.ts";
import { postOrder } from "../src/trader.ts";
import { applyMarketMessage } from "../src/book-cache.ts";

const WARMUP = 2_000;
const RUNS = 20_000;
const FAST_LOOP_SAMPLES = 1_000;

const config: Config = {
  cities: [],
  maxStake: 4,
  minShares: 5,
  prod: false,
  dryRun: false,
  privateKey: "0x0000000000000000000000000000000000000000000000000000000000000000",
  funderAddress: undefined,
  signatureType: undefined,
  strategy: "no" as const,
  dailyPeakTrigger: true,
  peakTriggerHour: 16,
};

type SubmitRecorder = {
  arm(): void;
  firstUs(): number;
};

function percentile(sorted: number[], p: number): number {
  return sorted[Math.min(Math.floor(sorted.length * p), sorted.length - 1)]!;
}

function summarize(values: number[]): { min: number; p50: number; p95: number; max: number } {
  const sorted = [...values].sort((a, b) => a - b);
  return {
    min: sorted[0]!,
    p50: percentile(sorted, 0.50),
    p95: percentile(sorted, 0.95),
    max: sorted[sorted.length - 1]!,
  };
}

function makeBucket(tokenSuffix: string, tempC = 24): BucketState {
  return {
    tempC,
    lowerTemp: tempC,
    upperTemp: tempC,
    unit: "C",
    label: `${tempC}°C`,
    type: "exact",
    noTokenId: `token-${tokenSuffix}`,
    yesTokenId: `yes-token-${tokenSuffix}`,
    conditionId: `condition-${tokenSuffix}`,
    negRisk: false,
    bought: false,
    attempted: false,
    pendingBuy: false,
    peakBought: false,
  };
}

function makeBenchClob(): ClobClient & SubmitRecorder {
  let start = 0;
  let first = -1;
  const markSubmit = () => {
    if (first < 0) first = (performance.now() - start) * 1_000;
  };

  return {
    arm() {
      first = -1;
      start = performance.now();
    },
    firstUs() {
      if (first < 0) throw new Error("benchmark did not invoke a fake CLOB submit");
      return first;
    },
    async getClobMarketInfo() {
      return undefined;
    },
    async createOrder(userOrder: { tokenID: string }) {
      return { kind: "limit", tokenID: userOrder.tokenID };
    },
    async createMarketOrder(userOrder: { tokenID: string }) {
      return { kind: "market", tokenID: userOrder.tokenID };
    },
    async postOrder(order: unknown) {
      markSubmit();
      return { status: "matched", order };
    },
    async createAndPostMarketOrder(order: unknown) {
      markSubmit();
      return { status: "matched", order };
    },
  } as unknown as ClobClient & SubmitRecorder;
}

async function withMutedStdout<T>(fn: () => Promise<T>): Promise<T> {
  const originalWrite = process.stdout.write;
  process.stdout.write = (() => true) as typeof process.stdout.write;
  try {
    return await fn();
  } finally {
    process.stdout.write = originalWrite;
  }
}

async function waitForPostOrderToSettle(bucket: BucketState): Promise<void> {
  for (let i = 0; bucket.pendingBuy && i < 10; i++) await Promise.resolve();
}

async function benchPreparedSubmit(clob: ClobClient & SubmitRecorder, bucket: BucketState): Promise<number[]> {
  const values: number[] = [];
  await withMutedStdout(async () => {
    for (let i = 0; i < WARMUP + RUNS; i++) {
      bucket.bought = false;
      bucket.attempted = false;
      bucket.pendingBuy = true;
      clob.arm();
      await postOrder(clob, bucket, config);
      const us = clob.firstUs();
      if (i >= WARMUP) values.push(us);
    }
  });
  return values;
}

async function benchColdFallback(clob: ClobClient & SubmitRecorder): Promise<number[]> {
  const values: number[] = [];
  const bucket = makeBucket(`cold-${Date.now()}`, 24);
  await withMutedStdout(async () => {
    for (let i = 0; i < WARMUP + RUNS; i++) {
      bucket.bought = false;
      bucket.attempted = false;
      bucket.pendingBuy = true;
      clob.arm();
      await postOrder(clob, bucket, config);
      const us = clob.firstUs();
      if (i >= WARMUP) values.push(us);
    }
  });
  return values;
}

async function benchHandleObsHotPath(clob: ClobClient & SubmitRecorder, bucket: BucketState): Promise<number[]> {
  const values: number[] = [];
  const bucketMap = new Map([[bucket.noTokenId, bucket]]);
  const buckets = [bucket];
  const obs: ObservationResult = { tempC: 25.2, observedAtUtcMs: 1_700_000_000_000 };
  const ref = { value: -Infinity };

  await withMutedStdout(async () => {
    for (let i = 0; i < WARMUP + RUNS; i++) {
      ref.value = -Infinity;
      bucket.bought = false;
      bucket.attempted = false;
      bucket.pendingBuy = false;
      clob.arm();
      handleObs(obs, "bench/SBGR", "SBGR", "America/Sao_Paulo", ref, { value: false }, bucketMap, buckets, clob, config);
      const us = clob.firstUs();
      if (i >= WARMUP) values.push(us);
      await waitForPostOrderToSettle(bucket);
    }
  });
  return values;
}

async function benchFastSourceGap(): Promise<number[]> {
  const gaps: number[] = [];
  let lastFast = 0;
  let fastCalls = 0;

  await runHotObservationLoops(
    [
      {
        source: "fast",
        fetch: async () => {
          fastCalls += 1;
          return { tempC: fastCalls, observedAtUtcMs: fastCalls };
        },
      },
      {
        source: "slow",
        fetch: signal => new Promise<ObservationResult | null>(resolve => {
          signal.addEventListener("abort", () => resolve(null), { once: true });
        }),
      },
    ],
    () => true,
    obs => obs.tempC >= FAST_LOOP_SAMPLES,
    (_obs, source) => {
      if (source !== "fast") return;
      const now = performance.now();
      if (lastFast !== 0) gaps.push((now - lastFast) * 1_000);
      lastFast = now;
    },
  );

  return gaps;
}

const clob = makeBenchClob();
const preparedBucket = makeBucket("prepared-primary", 24);
const handleBucket = makeBucket("handle-primary", 24);

await prepareOrders(clob, [preparedBucket, handleBucket], config);

// Populate book cache so prepared submit measures the real book-sweep path
applyMarketMessage({
  event_type: "book",
  asset_id: preparedBucket.noTokenId,
  asks: [{ price: "0.50", size: "10" }],
});

const prepared = summarize(await benchPreparedSubmit(clob, preparedBucket));
const cold = summarize(await benchColdFallback(clob));
const hot = summarize(await benchHandleObsHotPath(clob, handleBucket));
const fastGap = summarize(await benchFastSourceGap());

console.log("Execution latency benchmark (local fake CLOB, no network, no real orders)");
console.log(`  prepared submit: p50=${prepared.p50.toFixed(2)}us p95=${prepared.p95.toFixed(2)}us`);
console.log(`  handleObs hot path: min=${hot.min.toFixed(2)}us p50=${hot.p50.toFixed(2)}us p95=${hot.p95.toFixed(2)}us max=${hot.max.toFixed(2)}us`);
console.log(`  cold fallback: p50=${cold.p50.toFixed(2)}us p95=${cold.p95.toFixed(2)}us`);
console.log(`  fast source loop gap: p50=${fastGap.p50.toFixed(2)}us p95=${fastGap.p95.toFixed(2)}us`);
console.log(`METRIC hot_path_p50_us=${hot.p50.toFixed(3)}`);
console.log(`METRIC hot_path_p95_us=${hot.p95.toFixed(3)}`);
console.log(`METRIC hot_path_min_us=${hot.min.toFixed(3)}`);
console.log(`METRIC hot_path_max_us=${hot.max.toFixed(3)}`);
console.log(`METRIC prepared_submit_p50_us=${prepared.p50.toFixed(3)}`);
console.log(`METRIC prepared_submit_p95_us=${prepared.p95.toFixed(3)}`);
console.log(`METRIC cold_fallback_p50_us=${cold.p50.toFixed(3)}`);
console.log(`METRIC cold_fallback_p95_us=${cold.p95.toFixed(3)}`);
console.log(`METRIC hot_loop_fast_source_gap_us=${fastGap.p50.toFixed(3)}`);

export {};
