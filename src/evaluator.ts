import type { BucketState } from "./types.ts";
import { observedWholeTempInUnit } from "./markets.ts";

export function evaluateBuckets(
  observedMaxTempC: number,
  buckets: BucketState[],
  postOrder: (bucket: BucketState) => void,
): void {
  for (const b of buckets) {
    if (b.type !== "exact" && b.type !== "range") continue;
    const observedWhole = observedWholeTempInUnit(observedMaxTempC, b.unit);
    if (observedWhole <= b.upperTemp) continue;
    if (b.bought || b.attempted || b.pendingBuy) continue;
    b.pendingBuy = true;
    postOrder(b);
  }
}

function findPeakBuckets(observedMaxTempC: number, buckets: BucketState[]): {
  yesBucket: BucketState | undefined;
  noBucket: BucketState | undefined;
} {
  for (let i = 0; i < buckets.length; i++) {
    const b = buckets[i]!;
    if (b.type !== "exact") continue;

    const observedWhole = observedWholeTempInUnit(observedMaxTempC, b.unit);
    if (observedWhole < b.lowerTemp || observedWhole > b.upperTemp) continue;

    let noBucket: BucketState | undefined;
    for (let j = i + 1; j < buckets.length; j++) {
      const candidate = buckets[j]!;
      if (candidate.type === "exact" && candidate.unit === b.unit) {
        noBucket = candidate;
        break;
      }
    }

    return { yesBucket: b, noBucket };
  }

  return { yesBucket: undefined, noBucket: undefined };
}

// Fires once when the first METAR of `triggerHour` (city local time) arrives.
// Buys YES on the current ObservedMax bucket and NO on the bucket immediately above.
// Safety net for Peak Detection: whichever fires first wins via the shared peakTriggered flag.
// Expects buckets sorted ascending by tempC.
export function evaluatePeakAtHour(
  observedMaxTempC: number,
  buckets: BucketState[],
  metarLocalHour: number,
  triggerHour: number,
  peakTriggered: { value: boolean },
  postPeakOrders: (yesBucket: BucketState, noBucket: BucketState | undefined) => void,
): void {
  if (peakTriggered.value) return;
  if (metarLocalHour !== triggerHour) return;

  const { yesBucket, noBucket } = findPeakBuckets(observedMaxTempC, buckets);

  if (!yesBucket) return;
  if (yesBucket.peakBought) return;

  yesBucket.peakBought = true;
  peakTriggered.value = true;
  postPeakOrders(yesBucket, noBucket);
}

// Fires once when a confirmed temperature drop is detected between 12h–16h city-local time.
// Buys YES on the peak bucket and NO on the bucket immediately above.
// Expects buckets sorted ascending by tempC.
export function evaluatePeakDrop(
  observedMaxTempC: number,
  buckets: BucketState[],
  localHour: number,
  peakTriggered: { value: boolean },
  postPeakOrders: (yesBucket: BucketState, noBucket: BucketState | undefined) => void,
): void {
  if (peakTriggered.value) return;
  if (localHour < 12 || localHour >= 16) return;

  const { yesBucket, noBucket } = findPeakBuckets(observedMaxTempC, buckets);

  if (!yesBucket) return;
  if (yesBucket.peakBought) return;

  yesBucket.peakBought = true;
  peakTriggered.value = true;
  postPeakOrders(yesBucket, noBucket);
}
