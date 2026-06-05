import type { BucketState } from "./types.ts";

// Expects buckets sorted ascending by tempC for the early-break to work correctly.
export function evaluateBuckets(
  observedMaxTempC: number,
  buckets: BucketState[],
  postOrder: (bucket: BucketState) => void,
): void {
  const intMax = Math.floor(observedMaxTempC);
  for (const b of buckets) {
    if (b.type !== "exact") continue;
    if (intMax <= b.tempC) break; // sorted asc: all remaining are also not surpassed
    if (b.bought || b.attempted || b.pendingBuy) continue;
    b.pendingBuy = true;
    postOrder(b);
  }
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

  const peakFloor = Math.floor(observedMaxTempC);
  let yesBucket: BucketState | undefined;
  let noBucket: BucketState | undefined;

  for (let i = 0; i < buckets.length; i++) {
    const b = buckets[i]!;
    if (b.type !== "exact") continue;
    if (b.tempC === peakFloor) {
      yesBucket = b;
      for (let j = i + 1; j < buckets.length; j++) {
        if (buckets[j]!.type === "exact") { noBucket = buckets[j]; break; }
      }
      break;
    }
  }

  if (!yesBucket) return;
  if (yesBucket.peakBought) return;

  yesBucket.peakBought = true;
  peakTriggered.value = true;
  postPeakOrders(yesBucket, noBucket);
}

// Fires once when a confirmed temperature drop is detected between 12h–16h BRT.
// Buys YES on the peak bucket and NO on the bucket immediately above.
// Expects buckets sorted ascending by tempC.
export function evaluatePeakDrop(
  observedMaxTempC: number,
  buckets: BucketState[],
  brtHour: number,
  peakTriggered: { value: boolean },
  postPeakOrders: (yesBucket: BucketState, noBucket: BucketState | undefined) => void,
): void {
  if (peakTriggered.value) return;
  if (brtHour < 12 || brtHour >= 16) return;

  const peakFloor = Math.floor(observedMaxTempC);
  let yesBucket: BucketState | undefined;
  let noBucket: BucketState | undefined;

  for (let i = 0; i < buckets.length; i++) {
    const b = buckets[i]!;
    if (b.type !== "exact") continue;
    if (b.tempC === peakFloor) {
      yesBucket = b;
      // next exact bucket above peak is the NO target
      for (let j = i + 1; j < buckets.length; j++) {
        if (buckets[j]!.type === "exact") { noBucket = buckets[j]; break; }
      }
      break;
    }
  }

  if (!yesBucket) return;
  if (yesBucket.peakBought) return;

  yesBucket.peakBought = true;
  peakTriggered.value = true;
  postPeakOrders(yesBucket, noBucket);
}
