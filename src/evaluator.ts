import type { BucketState } from "./types.ts";

// Expects buckets sorted ascending by tempC for the early-break to work correctly.
export function evaluateBuckets(
  observedMaxTempC: number,
  buckets: BucketState[],
  postOrder: (tokenId: string) => void,
): void {
  const intMax = Math.floor(observedMaxTempC);
  for (const b of buckets) {
    if (b.type !== "exact") continue;
    if (intMax <= b.tempC) break; // sorted asc: all remaining are also not surpassed
    if (b.bought || b.attempted || b.pendingBuy) continue;
    postOrder(b.noTokenId);
    b.pendingBuy = true;
  }
}
