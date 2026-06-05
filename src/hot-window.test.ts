import { describe, expect, test } from "bun:test";
import type { ClobClient } from "@polymarket/clob-client-v2";
import type { Config } from "./config.ts";
import { prepareOrders } from "./order-cache.ts";
import type { BucketState } from "./types.ts";
import type { ObservationResult } from "./types.ts";
import { handleObs, msUntilNextHotWindow, processObservationFetches, runHotObservationLoops } from "./hot-window.ts";

const config: Config = {
  cities: [],
  maxStake: 4,
  minShares: 5,
  prod: false,
  dryRun: false,
  privateKey: "0x0",
  funderAddress: undefined,
  signatureType: undefined,
  strategy: "no",
  dailyPeakTrigger: true,
  peakTriggerHour: 17,
  tradeLedgerPath: ".state/test-trades.jsonl",
  metarMaxAgeMs: 900_000,
};

function deferred<T>(): {
  promise: Promise<T>;
  resolve: (value: T) => void;
} {
  let resolve!: (value: T) => void;
  const promise = new Promise<T>(res => { resolve = res; });
  return { promise, resolve };
}

function makeBucket(label: string, temp: number): BucketState {
  return {
    tempC: temp,
    lowerTemp: temp,
    upperTemp: temp,
    unit: "C",
    label,
    type: "exact",
    noTokenId: `no-${label}`,
    yesTokenId: `yes-${label}`,
    conditionId: `condition-${label}`,
    negRisk: false,
    bought: false,
    attempted: false,
    pendingBuy: false,
    peakBought: false,
  };
}

describe("handleObs", () => {
  test("can seed ObservedMax outside a Hot Window without posting Contested NO", async () => {
    const bucket = makeBucket("warm-no-trade-12", 12);
    let postOrderCalls = 0;

    const clob = {
      async getClobMarketInfo() {},
      async createOrder(userOrder: { tokenID: string }) {
        return { kind: "limit", tokenID: userOrder.tokenID };
      },
      async createMarketOrder(userOrder: { tokenID: string }) {
        return { kind: "market", tokenID: userOrder.tokenID };
      },
      async postOrder() {
        postOrderCalls += 1;
        return { status: "matched" };
      },
    } as unknown as ClobClient;

    await prepareOrders(clob, [bucket], config);

    const observedMaxRef = { value: -Infinity };
    handleObs(
      { tempC: 13, observedAtUtcMs: 1 },
      "warm/SBGR",
      "SBGR",
      "America/Sao_Paulo",
      observedMaxRef,
      { value: false },
      new Map([[bucket.noTokenId, bucket]]),
      [bucket],
      clob,
      config,
      false,
    );
    await Bun.sleep(0);

    expect(observedMaxRef.value).toBe(13);
    expect(postOrderCalls).toBe(0);
    expect(bucket.pendingBuy).toBe(false);
  });

  test("does not fire Daily Peak Trigger before the configured 17h hour", async () => {
    const buckets = [
      makeBucket("daily-trigger-off-22", 22),
      makeBucket("daily-trigger-off-23", 23),
    ];
    let postOrderCalls = 0;

    const clob = {
      async getClobMarketInfo() {},
      async createOrder(userOrder: { tokenID: string }) {
        return { kind: "limit", tokenID: userOrder.tokenID };
      },
      async createMarketOrder(userOrder: { tokenID: string }) {
        return { kind: "market", tokenID: userOrder.tokenID };
      },
      async postOrder() {
        postOrderCalls += 1;
        return { status: "matched" };
      },
    } as unknown as ClobClient;

    const peakConfig = { ...config, strategy: "peak" as const, metarMaxAgeMs: Number.MAX_SAFE_INTEGER };
    await prepareOrders(clob, buckets, peakConfig);

    const peakTriggered = { value: false };
    handleObs(
      { tempC: 22, observedAtUtcMs: Date.UTC(2026, 5, 5, 12, 0) },
      "aw/LTFM",
      "LTFM",
      "Europe/Istanbul",
      { value: -Infinity },
      peakTriggered,
      new Map(buckets.map(b => [b.noTokenId, b])),
      buckets,
      clob,
      peakConfig,
      true,
    );
    await Bun.sleep(0);

    expect(postOrderCalls).toBe(0);
    expect(peakTriggered.value).toBe(false);
  });

  test("fires Daily Peak Trigger at the configured 17h hour", async () => {
    const buckets = [
      makeBucket("daily-trigger-on-22", 22),
      makeBucket("daily-trigger-on-23", 23),
    ];
    let postOrderCalls = 0;

    const clob = {
      async getClobMarketInfo() {},
      async createOrder(userOrder: { tokenID: string }) {
        return { kind: "limit", tokenID: userOrder.tokenID };
      },
      async createMarketOrder(userOrder: { tokenID: string }) {
        return { kind: "market", tokenID: userOrder.tokenID };
      },
      async postOrder() {
        postOrderCalls += 1;
        return { status: "matched" };
      },
    } as unknown as ClobClient;

    const peakConfig = { ...config, strategy: "peak" as const, metarMaxAgeMs: Number.MAX_SAFE_INTEGER };
    await prepareOrders(clob, buckets, peakConfig);

    const peakTriggered = { value: false };
    handleObs(
      { tempC: 22, observedAtUtcMs: Date.UTC(2026, 5, 5, 14, 0) },
      "aw/LTFM",
      "LTFM",
      "Europe/Istanbul",
      { value: -Infinity },
      peakTriggered,
      new Map(buckets.map(b => [b.noTokenId, b])),
      buckets,
      clob,
      peakConfig,
      true,
    );
    await Bun.sleep(0);

    expect(postOrderCalls).toBe(2);
    expect(peakTriggered.value).toBe(true);
  });

  test("skips trades when METAR is stale", async () => {
    const bucket = makeBucket("stale-no-trade-12", 12);
    let postOrderCalls = 0;

    const clob = {
      async getClobMarketInfo() {},
      async createOrder(userOrder: { tokenID: string }) {
        return { kind: "limit", tokenID: userOrder.tokenID };
      },
      async createMarketOrder(userOrder: { tokenID: string }) {
        return { kind: "market", tokenID: userOrder.tokenID };
      },
      async postOrder() {
        postOrderCalls += 1;
        return { status: "matched" };
      },
    } as unknown as ClobClient;

    const staleConfig = { ...config, metarMaxAgeMs: 60_000 };
    await prepareOrders(clob, [bucket], staleConfig);

    const observedMaxRef = { value: -Infinity };
    handleObs(
      { tempC: 13, observedAtUtcMs: Date.now() - 120_000 },
      "stale/SBGR",
      "SBGR",
      "America/Sao_Paulo",
      observedMaxRef,
      { value: false },
      new Map([[bucket.noTokenId, bucket]]),
      [bucket],
      clob,
      staleConfig,
      true,
    );
    await Bun.sleep(0);

    expect(observedMaxRef.value).toBe(13);
    expect(postOrderCalls).toBe(0);
    expect(bucket.pendingBuy).toBe(false);
  });
});

describe("msUntilNextHotWindow", () => {
  test("uses the city's timezone to schedule the next local Hot Window", () => {
    const city = {
      slug: "los-angeles",
      icao: "KLAX",
      timezone: "America/Los_Angeles",
      targetHours: [11],
      hotWindowStart: 52,
      hotWindowEnd: 57,
    };

    expect(msUntilNextHotWindow(Date.UTC(2026, 5, 5, 17, 51), city)).toBe(60_000);
    expect(msUntilNextHotWindow(Date.UTC(2026, 5, 5, 17, 53), city)).toBe(0);
    expect(msUntilNextHotWindow(Date.UTC(2026, 5, 5, 14, 51), city)).toBe(10_860_000);
  });
});

describe("processObservationFetches", () => {
  test("handles each source as soon as that source resolves", async () => {
    const slow = deferred<ObservationResult | null>();
    const calls: string[] = [];

    processObservationFetches(
      [
        {
          source: "fast",
          promise: Promise.resolve({ tempC: 21, observedAtUtcMs: 1 }),
        },
        {
          source: "slow",
          promise: slow.promise,
        },
      ],
      (obs, source) => {
        calls.push(`${source}:${obs?.tempC ?? "null"}`);
      },
    );

    await Bun.sleep(0);

    expect(calls).toEqual(["fast:21"]);

    slow.resolve({ tempC: 22, observedAtUtcMs: 2 });
    await Bun.sleep(0);
    expect(calls).toEqual(["fast:21", "slow:22"]);
  });
});

describe("runHotObservationLoops", () => {
  test("continues polling a fast source while another source is still pending", async () => {
    let fastCalls = 0;
    const calls: string[] = [];

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
      obs => obs.tempC === 2,
      (obs, source) => calls.push(`${source}:${obs?.tempC ?? "null"}`),
    );

    expect(fastCalls).toBe(2);
    expect(calls).toEqual(["fast:1", "fast:2"]);
  });
});
