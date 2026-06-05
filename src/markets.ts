import type { BucketState, TemperatureUnit } from "./types.ts";

export interface GammaMarket {
  question: string;
  groupItemTitle?: string;
  clobTokenIds: string;
  conditionId: string;
}

export interface GammaEvent {
  negRisk: boolean;
  markets: GammaMarket[];
}

type ParsedBucket = {
  type: BucketState["type"];
  lowerTemp: number;
  upperTemp: number;
  unit: TemperatureUnit;
  label: string;
};

function parseUnit(unit: string): TemperatureUnit {
  return unit.toUpperCase() === "F" ? "F" : "C";
}

function parseTemperatureBucket(market: GammaMarket): ParsedBucket | null {
  const text = `${market.groupItemTitle ?? ""} ${market.question}`.replace(/\s+/g, " ").trim();
  const label = market.groupItemTitle?.trim();

  const range = text.match(/(-?\d+)\s*-\s*(-?\d+)\s*°\s*([CF])/i);
  if (range) {
    const lowerTemp = Number(range[1]);
    const upperTemp = Number(range[2]);
    const unit = parseUnit(range[3]!);
    return {
      type: "range",
      lowerTemp,
      upperTemp,
      unit,
      label: label ?? `${lowerTemp}-${upperTemp}°${unit}`,
    };
  }

  const single = text.match(/(-?\d+)\s*°\s*([CF])/i);
  if (!single) return null;

  const temp = Number(single[1]);
  const unit = parseUnit(single[2]!);
  const isBelow = /or below/i.test(text);
  const isAbove = /or higher/i.test(text);

  return {
    type: isBelow ? "below" : isAbove ? "above" : "exact",
    lowerTemp: temp,
    upperTemp: isAbove ? Infinity : temp,
    unit,
    label: label ?? `${temp}°${unit}${isBelow ? " or below" : isAbove ? " or higher" : ""}`,
  };
}

export function parseGammaMarket(event: GammaEvent, market: GammaMarket): BucketState | null {
  const parsed = parseTemperatureBucket(market);
  if (!parsed) return null;

  let tokenIds: [string, string];
  try {
    tokenIds = JSON.parse(market.clobTokenIds) as [string, string];
  } catch {
    return null;
  }

  const [yesTokenId, noTokenId] = tokenIds;
  if (!yesTokenId || !noTokenId) return null;

  return {
    tempC: parsed.upperTemp === Infinity ? parsed.lowerTemp : parsed.upperTemp,
    lowerTemp: parsed.lowerTemp,
    upperTemp: parsed.upperTemp,
    unit: parsed.unit,
    label: parsed.label,
    type: parsed.type,
    noTokenId,
    yesTokenId,
    conditionId: market.conditionId,
    negRisk: event.negRisk,
    bought: false,
    attempted: false,
    pendingBuy: false,
    peakBought: false,
  };
}

export function observedWholeTempInUnit(observedMaxTempC: number, unit: TemperatureUnit): number {
  const value = unit === "F" ? observedMaxTempC * 9 / 5 + 32 : observedMaxTempC;
  return Math.floor(value);
}

function sortValueC(bucket: BucketState): number {
  const value = bucket.upperTemp === Infinity ? bucket.lowerTemp : bucket.upperTemp;
  return bucket.unit === "F" ? (value - 32) * 5 / 9 : value;
}

export function sortBuckets(buckets: BucketState[]): void {
  buckets.sort((a, b) => sortValueC(a) - sortValueC(b));
}

export function formatBucket(bucket: BucketState): string {
  return bucket.label || `${bucket.tempC}°${bucket.unit}`;
}
