import { type ClobClient, Side, OrderType } from "@polymarket/clob-client-v2";
import type { BucketState } from "./types.ts";
import type { Config } from "./config.ts";
import { getCachedAsks } from "./book-cache.ts";
import { log } from "./logger.ts";

export async function postOrder(
  clob: ClobClient,
  bucket: BucketState,
  config: Config,
): Promise<void> {
  try {
    // Hot path: use cached order book — no HTTP call here.
    // getCachedAsks returns null on cache miss (stale/not yet populated).
    // null  → blind FOK without marking attempted (cache may just be cold)
    // []    → empty book confirmed → blind FOK + mark attempted
    const cachedAsks = getCachedAsks(bucket.noTokenId);
    const asks = cachedAsks ?? [];

    let shares = 0;
    let cost = 0;
    let price = 0.99;
    let blind = false;

    if (asks.length === 0) {
      blind = true;
      if (cachedAsks !== null) bucket.attempted = true; // genuinely empty book
      shares = config.maxStake / 0.99;
      cost = config.maxStake;
      price = 0.99;
    } else {
      let budget = config.maxStake;
      for (const ask of asks) {
        const p = parseFloat(ask.price);
        const s = parseFloat(ask.size);
        const levelCost = p * s;
        if (budget >= levelCost) {
          shares += s;
          cost += levelCost;
          budget -= levelCost;
          price = p;
        } else {
          shares += budget / p;
          cost += budget;
          price = p;
          break;
        }
      }
    }

    if (shares < config.minShares && !blind) {
      log("trader", `skip tokenId=${bucket.noTokenId} tempC=${bucket.tempC} reason=insufficient-liquidity shares=${shares.toFixed(2)}`);
      return;
    }

    if (cost < 1.0 && config.prod) {
      log("trader", `skip tokenId=${bucket.noTokenId} tempC=${bucket.tempC} reason=below-min-cost cost=${cost.toFixed(4)}`);
      return;
    }

    const priceRounded = Math.round(price * 100) / 100;
    const sharesRounded = Math.round(shares * 100) / 100;
    const costRounded = (priceRounded * sharesRounded).toFixed(4);

    log("trader", `attempt tokenId=${bucket.noTokenId} tempC=${bucket.tempC} price=${priceRounded} shares=${sharesRounded} cost=${costRounded} blind=${blind}${config.dryRun ? " [DRY RUN]" : ""}`);

    if (config.dryRun) {
      bucket.bought = true;
      return;
    }

    const order = await clob.createOrder(
      {
        tokenID: bucket.noTokenId,
        price: priceRounded,
        size: sharesRounded,
        side: Side.BUY,
      },
      { tickSize: "0.01", negRisk: bucket.negRisk },
    );

    const resp = await clob.postOrder(order, OrderType.FOK);
    const status: string = resp?.status ?? "unknown";

    log("trader", `result=${status} tokenId=${bucket.noTokenId} tempC=${bucket.tempC}`);

    if (status === "matched") {
      bucket.bought = true;
    }
  } catch (err) {
    log("trader", `error tokenId=${bucket.noTokenId} tempC=${bucket.tempC} err=${err}`);
  } finally {
    bucket.pendingBuy = false;
  }
}
