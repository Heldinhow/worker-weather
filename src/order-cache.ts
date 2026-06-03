import { type ClobClient, Side } from "@polymarket/clob-client-v2";
import type { Config } from "./config.ts";
import type { BucketState } from "./types.ts";
import { log } from "./logger.ts";

export const LIMIT_PRICE = 0.99;

type LimitOrder = Awaited<ReturnType<ClobClient["createOrder"]>>;
type MarketOrder = Awaited<ReturnType<ClobClient["createMarketOrder"]>>;

export type PreparedOrders = {
  limitOrder: LimitOrder;
  marketOrder: MarketOrder;
  shares: number;
};

const preparedOrders = new Map<string, PreparedOrders>();
const preparingOrders = new Map<string, Promise<void>>();

function gcd(a: number, b: number): number {
  while (b) { [a, b] = [b, a % b]; }
  return a;
}

// FAK orders require price * shares to have <= 2 decimal places.
// For price p = a/100, the minimum valid share step is 1/gcd(a,100).
export function snapShares(shares: number, price: number): number {
  if (price === LIMIT_PRICE) return Math.floor(shares);
  const a = Math.round(price * 100);
  const step = 1 / gcd(a, 100);
  return Math.floor(shares / step) * step;
}

const LIMIT_SHARE_STEP = 1 / gcd(Math.round(LIMIT_PRICE * 100), 100);

export function limitShares(config: Config): number {
  const shares = config.maxStake / LIMIT_PRICE;
  return Math.max(config.minShares, Math.floor(shares / LIMIT_SHARE_STEP) * LIMIT_SHARE_STEP);
}

export function getPreparedOrders(tokenId: string): PreparedOrders | undefined {
  return preparedOrders.get(tokenId);
}

export async function prepareOrders(
  clob: ClobClient,
  buckets: BucketState[],
  config: Config,
): Promise<void> {
  const exactRequested = buckets.filter(b => b.type === "exact");
  const pendingTasks = exactRequested
    .map(b => preparingOrders.get(b.noTokenId))
    .filter((task): task is Promise<void> => task !== undefined);
  const exactBuckets = buckets.filter(b =>
    b.type === "exact" &&
    !preparedOrders.has(b.noTokenId) &&
    !preparingOrders.has(b.noTokenId)
  );
  if (exactBuckets.length === 0) {
    await Promise.allSettled(pendingTasks);
    return;
  }

  const startedAt = performance.now();
  const shares = limitShares(config);

  const tasks = exactBuckets.map(bucket => {
    const options = { tickSize: "0.01" as const, negRisk: bucket.negRisk };
    const task = (async () => {
      await clob.getClobMarketInfo(bucket.conditionId).catch(() => undefined);
      const [limitOrder, marketOrder] = await Promise.all([
        clob.createOrder(
          { tokenID: bucket.noTokenId, price: LIMIT_PRICE, size: shares, side: Side.BUY },
          options,
        ),
        clob.createMarketOrder(
          { tokenID: bucket.noTokenId, amount: config.maxStake, price: LIMIT_PRICE, side: Side.BUY },
          options,
        ),
      ]);

      preparedOrders.set(bucket.noTokenId, { limitOrder, marketOrder, shares });
    })().finally(() => {
      preparingOrders.delete(bucket.noTokenId);
    });

    preparingOrders.set(bucket.noTokenId, task);
    return task;
  });

  const results = await Promise.allSettled([...pendingTasks, ...tasks]);

  const ok = results.filter(r => r.status === "fulfilled").length;
  const failed = results.length - ok;
  log("prewarm", `prepared orders ok=${ok}/${results.length}${failed ? ` failed=${failed}` : ""} ms=${(performance.now() - startedAt).toFixed(1)}`);
}
