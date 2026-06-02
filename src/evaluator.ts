import type { BucketState } from "./types.ts";

export function evaluateBuckets(
  observedMaxTempC: number,
  buckets: BucketState[],
  postOrder: (tokenId: string) => void,
): void {
  const intMax = Math.floor(observedMaxTempC);
  for (const b of buckets) {
    if (b.bought || b.attempted || b.pendingBuy) continue;
    if (b.type !== "exact") continue;
    if (intMax <= b.tempC) continue;
    postOrder(b.noTokenId);
    b.pendingBuy = true;
  }
}
