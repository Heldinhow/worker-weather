import { afterEach, describe, expect, test } from "bun:test";
import { mkdtempSync, readFileSync, rmSync } from "node:fs";
import { tmpdir } from "node:os";
import { dirname, join } from "node:path";
import type { BucketState } from "./types.ts";
import { getExecutedTrades, initTradeLedger, markExecutedBuckets, recordExecutedTrade, resetTradeLedgerForTests } from "./trade-ledger.ts";

function bucket(label: string, temp: number): BucketState {
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

function tempLedgerPath(): string {
  const dir = mkdtempSync(join(tmpdir(), "worker-weather-ledger-"));
  return join(dir, "trades.jsonl");
}

afterEach(() => {
  resetTradeLedgerForTests();
});

describe("trade ledger", () => {
  test("reloads matched NO trades and marks buckets as already bought", () => {
    const path = tempLedgerPath();
    const firstRun = bucket("20°C", 20);

    initTradeLedger(path);
    recordExecutedTrade({ bucket: firstRun, strategy: "contested-no", side: "NO", tokenId: firstRun.noTokenId, price: 0.99, shares: 5 });

    resetTradeLedgerForTests();
    initTradeLedger(path);
    const restarted = bucket("20°C", 20);
    markExecutedBuckets([restarted]);

    expect(restarted.bought).toBe(true);
    expect(getExecutedTrades()).toHaveLength(1);

    rmSync(dirname(path), { recursive: true, force: true });
  });

  test("reloads matched YES trades and marks peak buckets as already bought", () => {
    const path = tempLedgerPath();
    const firstRun = bucket("21°C", 21);

    initTradeLedger(path);
    recordExecutedTrade({ bucket: firstRun, strategy: "peak-yes", side: "YES", tokenId: firstRun.yesTokenId, price: 0.95, shares: 5.2 });

    resetTradeLedgerForTests();
    initTradeLedger(path);
    const restarted = bucket("21°C", 21);
    markExecutedBuckets([restarted]);

    expect(restarted.peakBought).toBe(true);
    expect(getExecutedTrades()[0]!.strategy).toBe("peak-yes");

    rmSync(dirname(path), { recursive: true, force: true });
  });

  test("deduplicates repeated records for the same token", () => {
    const path = tempLedgerPath();
    const traded = bucket("22°C", 22);

    initTradeLedger(path);
    recordExecutedTrade({ bucket: traded, strategy: "contested-no", side: "NO", tokenId: traded.noTokenId, price: 0.99, shares: 5 });
    recordExecutedTrade({ bucket: traded, strategy: "contested-no", side: "NO", tokenId: traded.noTokenId, price: 0.99, shares: 5 });

    expect(getExecutedTrades()).toHaveLength(1);
    expect(readFileSync(path, "utf8").trim().split("\n")).toHaveLength(1);

    rmSync(dirname(path), { recursive: true, force: true });
  });
});
