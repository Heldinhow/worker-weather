import { describe, expect, test } from "bun:test";
import type { ClobClient } from "@polymarket/clob-client-v2";
import type { Config } from "./config.ts";
import type { BucketState } from "./types.ts";
import { getPreparedOrders, limitShares, prepareOrders } from "./order-cache.ts";

const config: Config = {
  cities: [],
  maxStake: 4,
  minShares: 5,
  prod: false,
  dryRun: false,
  privateKey: "0x0",
  funderAddress: undefined,
  signatureType: undefined,
  strategy: "no" as const,
};

const exactBucket: BucketState = {
  tempC: 20,
  type: "exact",
  noTokenId: "token-20",
  yesTokenId: "yes-token-20",
  conditionId: "condition-20",
  negRisk: true,
  bought: false,
  attempted: false,
  pendingBuy: false,
  peakBought: false,
};

describe("limitShares", () => {
  test("uses the same 0.99 FAK share snapping semantics", () => {
    expect(limitShares({ ...config, maxStake: 4, minShares: 1 })).toBe(4);
    expect(limitShares({ ...config, maxStake: 4, minShares: 5 })).toBe(5);
  });
});

describe("prepareOrders", () => {
  test("pre-warms exact buckets and stores signed limit and market orders", async () => {
    const calls: string[] = [];
    const clob = {
      async getClobMarketInfo(conditionId: string) {
        calls.push(`info:${conditionId}`);
      },
      async createOrder(userOrder: { tokenID: string; price: number; size: number }) {
        calls.push(`limit:${userOrder.tokenID}:${userOrder.price}:${userOrder.size}`);
        return { kind: "limit", tokenID: userOrder.tokenID };
      },
      async createMarketOrder(userOrder: { tokenID: string; amount: number; price?: number }) {
        calls.push(`market:${userOrder.tokenID}:${userOrder.amount}:${userOrder.price}`);
        return { kind: "market", tokenID: userOrder.tokenID };
      },
    } as unknown as ClobClient;

    await prepareOrders(clob, [
      exactBucket,
      { ...exactBucket, type: "below", noTokenId: "token-below" },
    ], config);

    expect(calls).toContain("info:condition-20");
    expect(calls).toContain("limit:token-20:0.99:5");
    expect(calls).toContain("market:token-20:4:0.99");
    expect(calls.some(call => call.includes("token-below"))).toBe(false);
    expect(getPreparedOrders("token-20") as unknown).toEqual({
      limitOrder: { kind: "limit", tokenID: "token-20" },
      marketOrder: { kind: "market", tokenID: "token-20" },
      shares: 5,
    });
  });

  test("awaits in-flight preparation instead of starting duplicate signing", async () => {
    let releaseLimit!: () => void;
    const limitReady = new Promise<void>(resolve => { releaseLimit = resolve; });
    let createOrderCalls = 0;

    const bucket: BucketState = {
      ...exactBucket,
      noTokenId: "token-inflight",
      conditionId: "condition-inflight",
    };

    const clob = {
      async getClobMarketInfo() {},
      async createOrder(userOrder: { tokenID: string }) {
        createOrderCalls += 1;
        await limitReady;
        return { kind: "limit", tokenID: userOrder.tokenID };
      },
      async createMarketOrder(userOrder: { tokenID: string }) {
        return { kind: "market", tokenID: userOrder.tokenID };
      },
    } as unknown as ClobClient;

    const first = prepareOrders(clob, [bucket], config);
    await Bun.sleep(0);
    const second = prepareOrders(clob, [bucket], config);

    expect(createOrderCalls).toBe(1);
    releaseLimit();
    await Promise.all([first, second]);

    expect(createOrderCalls).toBe(1);
    expect(getPreparedOrders("token-inflight")?.shares).toBe(5);
  });
});
