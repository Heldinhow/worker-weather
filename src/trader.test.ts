import { describe, expect, test } from "bun:test";
import type { ClobClient } from "@polymarket/clob-client-v2";
import type { Config } from "./config.ts";
import type { BucketState } from "./types.ts";
import { prepareOrders } from "./order-cache.ts";
import { postOrder } from "./trader.ts";

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
  dailyPeakTrigger: true,
  peakTriggerHour: 16,
};

describe("postOrder", () => {
  test("uses prepared blind limit and market orders without signing on trigger", async () => {
    const bucket: BucketState = {
      tempC: 20,
      lowerTemp: 20,
      upperTemp: 20,
      unit: "C",
      label: "20°C",
      type: "exact",
      noTokenId: "token-trader",
      yesTokenId: "yes-token-trader",
      conditionId: "condition-trader",
      negRisk: false,
      bought: false,
      attempted: false,
      pendingBuy: true,
      peakBought: false,
    };

    const posted: unknown[] = [];
    let createOrderCalls = 0;
    let createAndPostMarketOrderCalls = 0;

    const clob = {
      async getClobMarketInfo() {},
      async createOrder(userOrder: { tokenID: string }) {
        createOrderCalls += 1;
        return { kind: "limit", tokenID: userOrder.tokenID };
      },
      async createMarketOrder(userOrder: { tokenID: string }) {
        return { kind: "market", tokenID: userOrder.tokenID };
      },
      async postOrder(order: unknown) {
        posted.push(order);
        return { status: "matched" };
      },
      async createAndPostMarketOrder() {
        createAndPostMarketOrderCalls += 1;
        throw new Error("createAndPostMarketOrder should not run when prepared orders exist");
      },
    } as unknown as ClobClient;

    await prepareOrders(clob, [bucket], config);
    createOrderCalls = 0;

    await postOrder(clob, bucket, config);

    expect(createOrderCalls).toBe(0);
    expect(createAndPostMarketOrderCalls).toBe(0);
    expect(posted).toEqual([
      { kind: "limit", tokenID: "token-trader" },
    ]);
    expect(bucket.bought).toBe(true);
    expect(bucket.pendingBuy).toBe(false);
  });
});
