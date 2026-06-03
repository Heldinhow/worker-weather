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
