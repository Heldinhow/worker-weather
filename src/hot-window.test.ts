import { describe, expect, test } from "bun:test";
import type { ObservationResult } from "./types.ts";
import { processObservationFetches, runHotObservationLoops } from "./hot-window.ts";

function deferred<T>(): {
  promise: Promise<T>;
  resolve: (value: T) => void;
} {
  let resolve!: (value: T) => void;
  const promise = new Promise<T>(res => { resolve = res; });
  return { promise, resolve };
}

describe("processObservationFetches", () => {
  test("handles each source as soon as that source resolves", async () => {
    const slow = deferred<ObservationResult | null>();
    const calls: string[] = [];

    const all = processObservationFetches(
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
    await expect(all).resolves.toEqual([
      { tempC: 21, observedAtUtcMs: 1 },
      { tempC: 22, observedAtUtcMs: 2 },
    ]);
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
