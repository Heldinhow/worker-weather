import { appendFileSync, existsSync, mkdirSync, readFileSync } from "node:fs";
import { dirname } from "node:path";
import type { BucketState, TemperatureUnit } from "./types.ts";
import { formatBucket } from "./markets.ts";
import { formatBrt } from "./time.ts";

export type TradeStrategy = "contested-no" | "peak-yes" | "peak-no";
export type TradeSide = "YES" | "NO";

export type TradeRecord = {
  version: 1;
  recordedAtMs: number;
  recordedAtBrt: string;
  icao: string;
  citySlug: string;
  eventSlug: string;
  strategy: TradeStrategy;
  side: TradeSide;
  tokenId: string;
  conditionId: string;
  bucket: string;
  unit: TemperatureUnit;
  lowerTemp: number;
  upperTemp: number;
  price: number;
  shares: number | undefined;
  status: "matched";
};

type RecordTradeInput = {
  bucket: BucketState;
  strategy: TradeStrategy;
  side: TradeSide;
  tokenId: string;
  price: number;
  shares: number | undefined;
};

let initialized = false;
let ledgerPath: string | undefined;
const tokenIds = new Set<string>();
const records: TradeRecord[] = [];

function isTradeRecord(value: unknown): value is TradeRecord {
  const record = value as Partial<TradeRecord> | undefined;
  return record?.version === 1 &&
    record.status === "matched" &&
    typeof record.tokenId === "string" &&
    typeof record.strategy === "string" &&
    typeof record.side === "string";
}

function remember(record: TradeRecord): void {
  if (tokenIds.has(record.tokenId)) return;
  tokenIds.add(record.tokenId);
  records.push(record);
}

export function initTradeLedger(path: string): void {
  initialized = true;
  ledgerPath = path;
  tokenIds.clear();
  records.length = 0;

  if (!existsSync(path)) return;

  const lines = readFileSync(path, "utf8").split("\n");
  for (const line of lines) {
    const trimmed = line.trim();
    if (!trimmed) continue;
    try {
      const parsed = JSON.parse(trimmed) as unknown;
      if (isTradeRecord(parsed)) remember(parsed);
    } catch {
      // Ignore corrupt historical lines instead of blocking the bot at boot.
    }
  }
}

export function hasExecutedTrade(tokenId: string): boolean {
  return initialized && tokenIds.has(tokenId);
}

export function getExecutedTrades(limit = Infinity): TradeRecord[] {
  const source = Number.isFinite(limit) ? records.slice(-limit) : records;
  return [...source];
}

export function recordExecutedTrade(input: RecordTradeInput): TradeRecord | undefined {
  if (!initialized || tokenIds.has(input.tokenId)) return undefined;

  const recordedAtMs = Date.now();
  const record: TradeRecord = {
    version: 1,
    recordedAtMs,
    recordedAtBrt: formatBrt(recordedAtMs),
    icao: input.bucket.icao ?? "?",
    citySlug: input.bucket.citySlug ?? "?",
    eventSlug: input.bucket.eventSlug ?? "?",
    strategy: input.strategy,
    side: input.side,
    tokenId: input.tokenId,
    conditionId: input.bucket.conditionId,
    bucket: formatBucket(input.bucket),
    unit: input.bucket.unit,
    lowerTemp: input.bucket.lowerTemp,
    upperTemp: input.bucket.upperTemp,
    price: input.price,
    shares: input.shares,
    status: "matched",
  };

  remember(record);

  if (ledgerPath) {
    mkdirSync(dirname(ledgerPath), { recursive: true });
    appendFileSync(ledgerPath, `${JSON.stringify(record)}\n`);
  }

  return record;
}

export function markExecutedBuckets(buckets: BucketState[]): void {
  if (!initialized) return;

  for (const bucket of buckets) {
    if (tokenIds.has(bucket.noTokenId)) bucket.bought = true;
    if (tokenIds.has(bucket.yesTokenId)) bucket.peakBought = true;
  }
}

export function resetTradeLedgerForTests(): void {
  initialized = false;
  ledgerPath = undefined;
  tokenIds.clear();
  records.length = 0;
}
