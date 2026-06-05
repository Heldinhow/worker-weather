import { afterEach, describe, expect, test } from "bun:test";
import type { ClobClient } from "@polymarket/clob-client-v2";
import { mkdtempSync, rmSync } from "node:fs";
import { tmpdir } from "node:os";
import { dirname, join } from "node:path";
import type { Config } from "./config.ts";
import type { BucketState } from "./types.ts";
import { prepareOrders } from "./order-cache.ts";
import { postOrder } from "./trader.ts";
import { initTradeLedger, recordExecutedTrade, resetTradeLedgerForTests } from "./trade-ledger.ts";

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
  peakTriggerHour: 17,
  tradeLedgerPath: ".state/test-trades.jsonl",
  metarMaxAgeMs: 900_000,
};

function makeBucket(label: string, temp: number): BucketState {
  return {
    tempC: temp,
    lowerTemp: temp,
    upperTemp: temp,
    unit: "C",
    label,
    icao: "SBGR",
    citySlug: "sao-paulo",
    eventSlug: "highest-temperature-in-sao-paulo-on-june-5-2026",
    type: "exact",
    noTokenId: `token-${label}`,
    yesTokenId: `yes-token-${label}`,
    conditionId: `condition-${label}`,
    negRisk: false,
    bought: false,
    attempted: false,
    pendingBuy: true,
    peakBought: false,
  };
}

function tempLedgerPath(): string {
  const dir = mkdtempSync(join(tmpdir(), "worker-weather-trader-ledger-"));
  return join(dir, "trades.jsonl");
}

afterEach(() => {
  resetTradeLedgerForTests();
});

describe("postOrder", () => {
  test("uses prepared blind limit and market orders without signing on trigger", async () => {
    const bucket = makeBucket("trader", 20);

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

  test("skips CLOB posting when the NO token was already matched in the ledger", async () => {
    const path = tempLedgerPath();
    const traded = makeBucket("already-traded", 20);
    initTradeLedger(path);
    recordExecutedTrade({ bucket: traded, strategy: "contested-no", side: "NO", tokenId: traded.noTokenId, price: 0.99, shares: 5 });

    let postOrderCalls = 0;
    const clob = {
      async postOrder() {
        postOrderCalls += 1;
        return { status: "matched" };
      },
    } as unknown as ClobClient;

    traded.bought = false;
    traded.pendingBuy = true;
    await postOrder(clob, traded, config);

    expect(postOrderCalls).toBe(0);
    expect(traded.bought).toBe(true);
    expect(traded.pendingBuy).toBe(false);

    rmSync(dirname(path), { recursive: true, force: true });
  });
});
