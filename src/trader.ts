import { type ClobClient, Side, OrderType } from "@polymarket/clob-client-v2";
import type { BucketState } from "./types.ts";
import type { Config } from "./config.ts";
import { getCachedAsks } from "./book-cache.ts";
import { getPreparedOrders, limitShares, LIMIT_PRICE, snapShares } from "./order-cache.ts";
import { log } from "./logger.ts";

export async function postOrder(
  clob: ClobClient,
  bucket: BucketState,
  config: Config,
): Promise<void> {
  try {
    const cachedAsks = getCachedAsks(bucket.noTokenId);

    if (cachedAsks !== null && cachedAsks.length === 0) {
      await postLimitFak(clob, bucket, config, limitShares(config), "empty-cache-probe");
      return;
    }

    if (cachedAsks === null) {
      await postBlindExperiment(clob, bucket, config);
      return;
    }

    // Book available — sweep all asks with no price ceiling
    let shares = 0;
    let budget = config.maxStake;
    for (const ask of cachedAsks) {
      const p = parseFloat(ask.price);
      const s = parseFloat(ask.size);
      const levelCost = p * s;
      if (budget >= levelCost) {
        shares += s;
        budget -= levelCost;
      } else {
        shares += budget / p;
        break;
      }
    }

    const sharesRounded = Math.max(config.minShares, snapShares(shares, LIMIT_PRICE));

    await postLimitFak(clob, bucket, config, sharesRounded, "book-sweep");

  } catch (err) {
    log("trader", `error tokenId=${bucket.noTokenId} tempC=${bucket.tempC} err=${err}`);
  } finally {
    bucket.pendingBuy = false;
  }
}

async function postLimitFak(
  clob: ClobClient,
  bucket: BucketState,
  config: Config,
  shares: number,
  reason: string,
): Promise<void> {
  const prepared = getPreparedOrders(bucket.noTokenId);
  const orderShares = prepared?.shares ?? shares;

  if (config.dryRun) {
    log("trader", `attempt tokenId=${bucket.noTokenId} tempC=${bucket.tempC} reason=${reason} price≤${LIMIT_PRICE} shares=${orderShares} [DRY RUN]`);
    bucket.bought = true;
    return;
  }

  const order = prepared
    ? prepared.limitOrder
    : await clob.createOrder(
      { tokenID: bucket.noTokenId, price: LIMIT_PRICE, size: shares, side: Side.BUY },
      { tickSize: "0.01", negRisk: bucket.negRisk },
    );

  const submit = clob.postOrder(order, OrderType.FAK);
  log("trader", `attempt tokenId=${bucket.noTokenId} tempC=${bucket.tempC} reason=${reason} price≤${LIMIT_PRICE} shares=${orderShares}${prepared ? " prepared=true" : ""}`);
  const resp = await submit;
  logResult("trader", resp, bucket);
  if (String(resp?.status ?? "") === "matched") bucket.bought = true;
}

function postBlindExperiment(
  clob: ClobClient,
  bucket: BucketState,
  config: Config,
): Promise<void> {
  const shares = limitShares(config);

  if (config.dryRun) {
    log("trader", `blind attempt tokenId=${bucket.noTokenId} tempC=${bucket.tempC} limit-fak price=${LIMIT_PRICE} shares=${shares} market-fak amount=${config.maxStake} [DRY RUN]`);
    bucket.bought = true;
    return Promise.resolve();
  }

  const prepared = getPreparedOrders(bucket.noTokenId);
  const limitSubmit = (async () => {
    const order = prepared?.limitOrder ?? await clob.createOrder(
      { tokenID: bucket.noTokenId, price: LIMIT_PRICE, size: shares, side: Side.BUY },
      { tickSize: "0.01", negRisk: bucket.negRisk },
    );
    return clob.postOrder(order, OrderType.FAK);
  })();
  const marketSubmit = prepared?.marketOrder
    ? clob.postOrder(prepared.marketOrder, OrderType.FAK)
    : clob.createAndPostMarketOrder(
      { tokenID: bucket.noTokenId, amount: config.maxStake, price: LIMIT_PRICE, side: Side.BUY },
      { tickSize: "0.01", negRisk: bucket.negRisk },
      OrderType.FAK,
    );
  log("trader", `blind attempt tokenId=${bucket.noTokenId} tempC=${bucket.tempC} limit-fak price=${LIMIT_PRICE} shares=${shares} market-fak amount=${config.maxStake}${prepared ? " prepared=true" : ""}`);

  return Promise.allSettled([
    limitSubmit,
    marketSubmit,
  ]).then(([limitResult, marketResult]) => {
    for (const [label, result] of [["limit-fak", limitResult], ["market-fak", marketResult]] as const) {
      if (result.status === "fulfilled") {
        logResult(`trader/${label}`, result.value, bucket);
        if (String(result.value?.status ?? "") === "matched") bucket.bought = true;
      } else {
        log(`trader/${label}`, `error tokenId=${bucket.noTokenId} tempC=${bucket.tempC} err=${result.reason}`);
      }
    }
  });
}

function logResult(tag: string, resp: any, bucket: BucketState): void {
  const status = String(resp?.status ?? "unknown");
  const errDetail: string = resp?.errorMsg || resp?.error || "";
  log(tag, `result=${status}${errDetail ? ` msg="${errDetail}"` : ""} tokenId=${bucket.noTokenId} tempC=${bucket.tempC}`);
  // Book was empty at execution time — another bot swept it; no point retrying.
  if (status === "400" && errDetail.includes("no orders found")) bucket.attempted = true;
}
