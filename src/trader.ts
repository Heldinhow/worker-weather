import { type ClobClient, Side, OrderType } from "@polymarket/clob-client-v2";
import type { BucketState } from "./types.ts";
import type { Config } from "./config.ts";
import { getCachedAsks, getCachedAsksFast } from "./book-cache.ts";
import { getPreparedOrders, getPreparedYesOrder, getPreparedPeakNoOrder, limitShares, peakShares, LIMIT_PRICE, YES_LIMIT_PRICE, PEAK_NO_LIMIT_PRICE, snapShares, type PreparedOrders } from "./order-cache.ts";
import { log } from "./logger.ts";
import { formatBucket } from "./markets.ts";
import { hasExecutedTrade, recordExecutedTrade } from "./trade-ledger.ts";

const LIMIT_PRICE_STR = String(LIMIT_PRICE);
const CONTESTED_NO = "contested-no";
const PEAK_YES = "peak-yes";
const PEAK_NO = "peak-no";
const CONTESTED_NO_STRATEGY = `strategy=${CONTESTED_NO}`;
const PEAK_YES_STRATEGY = `strategy=${PEAK_YES}`;
const PEAK_NO_STRATEGY = `strategy=${PEAK_NO}`;

export async function postOrder(
  clob: ClobClient,
  bucket: BucketState,
  config: Config,
): Promise<void> {
  if (bucket.bought || hasExecutedTrade(bucket.noTokenId)) {
    bucket.bought = true;
    bucket.pendingBuy = false;
    return;
  }

  const prepared = getPreparedOrders(bucket.noTokenId);
  if (prepared) {
    // Fast path: prepared order ready — skip book analysis, sweep math, getCachedAsks branching and try/finally overhead.
    // When prepared orders exist, always use the prepared limit FAK regardless of book state.
    // This is strictly better than blind experiment (which fires an extra market FAK) and
    // eliminates all cache-state branches from the hot path.
    const t0 = performance.now();
    if (config.dryRun) {
      log("trader", `attempt ${CONTESTED_NO_STRATEGY} tokenId=${bucket.noTokenId} bucket=${formatBucket(bucket)} reason=prepared price≤${LIMIT_PRICE_STR} shares=${prepared.shares} [DRY RUN]`);
      bucket.bought = true;
    } else {
      const submit = clob.postOrder(prepared.limitOrder, OrderType.FAK);
      const elapsedUs = Math.round((performance.now() - t0) * 1000);
      log("trader", `attempt ${CONTESTED_NO_STRATEGY} tokenId=${bucket.noTokenId} bucket=${formatBucket(bucket)} reason=prepared price≤${LIMIT_PRICE_STR} shares=${prepared.shares} prepared=true hotPath=${elapsedUs}µs`);
      const resp = await submit;
      logResult("trader", resp, bucket);
      if (String(resp?.status ?? "") === "matched") markMatchedNo(bucket, LIMIT_PRICE, prepared.shares);
    }
    bucket.pendingBuy = false;
    return;
  }

  // Slow path: no prepared orders
  try {
    const cachedAsks = getCachedAsks(bucket.noTokenId);

    if (cachedAsks !== null && cachedAsks.length === 0) {
      await postLimitFak(clob, bucket, config, limitShares(config), "empty-cache-probe");
      return;
    }

    if (cachedAsks === null) {
      await postBlindExperiment(clob, bucket, config, undefined);
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
    log("trader", `error ${CONTESTED_NO_STRATEGY} tokenId=${bucket.noTokenId} bucket=${formatBucket(bucket)} err=${err}`);
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
    log("trader", `attempt ${CONTESTED_NO_STRATEGY} tokenId=${bucket.noTokenId} bucket=${formatBucket(bucket)} reason=${reason} price≤${LIMIT_PRICE_STR} shares=${orderShares} [DRY RUN]`);
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
  log("trader", `attempt ${CONTESTED_NO_STRATEGY} tokenId=${bucket.noTokenId} bucket=${formatBucket(bucket)} reason=${reason} price≤${LIMIT_PRICE_STR} shares=${orderShares}${prepared ? " prepared=true" : ""}`);
  const resp = await submit;
  logResult("trader", resp, bucket);
  if (String(resp?.status ?? "") === "matched") markMatchedNo(bucket, LIMIT_PRICE, orderShares);
}

function postBlindExperiment(
  clob: ClobClient,
  bucket: BucketState,
  config: Config,
  prepared: PreparedOrders | undefined,
): Promise<void> {
  const shares = limitShares(config);

  if (config.dryRun) {
    log("trader", `blind attempt ${CONTESTED_NO_STRATEGY} tokenId=${bucket.noTokenId} bucket=${formatBucket(bucket)} limit-fak price=${LIMIT_PRICE_STR} shares=${shares} market-fak amount=${config.maxStake} [DRY RUN]`);
    bucket.bought = true;
    return Promise.resolve();
  }

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
  log("trader", `blind attempt ${CONTESTED_NO_STRATEGY} tokenId=${bucket.noTokenId} bucket=${formatBucket(bucket)} limit-fak price=${LIMIT_PRICE_STR} shares=${shares} market-fak amount=${config.maxStake}${prepared ? " prepared=true" : ""}`);

  return Promise.allSettled([
    limitSubmit,
    marketSubmit,
  ]).then(([limitResult, marketResult]) => {
    for (const [label, result] of [["limit-fak", limitResult], ["market-fak", marketResult]] as const) {
      if (result.status === "fulfilled") {
        logResult(`trader/${label}`, result.value, bucket);
        if (String(result.value?.status ?? "") === "matched") {
          markMatchedNo(bucket, LIMIT_PRICE, label === "limit-fak" ? shares : undefined);
        }
      } else {
        log(`trader/${label}`, `error ${CONTESTED_NO_STRATEGY} tokenId=${bucket.noTokenId} bucket=${formatBucket(bucket)} err=${result.reason}`);
      }
    }
  });
}

export function postPeakOrders(
  clob: ClobClient,
  yesBucket: BucketState,
  noBucket: BucketState | undefined,
  config: Config,
): void {
  void postPeakYes(clob, yesBucket, config);
  if (noBucket) void postPeakNo(clob, noBucket, config);
}

async function postPeakYes(clob: ClobClient, bucket: BucketState, config: Config): Promise<void> {
  if (hasExecutedTrade(bucket.yesTokenId)) {
    bucket.peakBought = true;
    return;
  }

  const prepared = getPreparedYesOrder(bucket.yesTokenId);
  if (config.dryRun) {
    log("peak/yes", `attempt ${PEAK_YES_STRATEGY} yesTokenId=${bucket.yesTokenId} bucket=${formatBucket(bucket)} price≤${YES_LIMIT_PRICE} shares=${prepared?.shares ?? "?"} [DRY RUN]`);
    return;
  }
  const t0 = performance.now();
  const fallbackShares = peakShares(YES_LIMIT_PRICE, config);
  const order = prepared?.limitOrder ?? await clob.createOrder(
    { tokenID: bucket.yesTokenId, price: YES_LIMIT_PRICE, size: fallbackShares, side: Side.BUY },
    { tickSize: "0.01", negRisk: bucket.negRisk },
  );
  const submit = clob.postOrder(order, OrderType.FAK);
  const elapsedUs = Math.round((performance.now() - t0) * 1000);
  const sharesDisplay = prepared?.shares ?? fallbackShares;
  log("peak/yes", `attempt ${PEAK_YES_STRATEGY} yesTokenId=${bucket.yesTokenId} bucket=${formatBucket(bucket)} price≤${YES_LIMIT_PRICE} shares=${sharesDisplay}${prepared ? " prepared=true" : ""} hotPath=${elapsedUs}µs`);
  const resp = await submit;
  logPeakResult("peak/yes", resp, bucket.yesTokenId, bucket);
  if (String(resp?.status ?? "") === "matched") {
    bucket.peakBought = true;
    recordExecutedTrade({ bucket, strategy: PEAK_YES, side: "YES", tokenId: bucket.yesTokenId, price: YES_LIMIT_PRICE, shares: sharesDisplay });
  }
}

async function postPeakNo(clob: ClobClient, bucket: BucketState, config: Config): Promise<void> {
  if (bucket.bought || hasExecutedTrade(bucket.noTokenId)) {
    bucket.bought = true;
    return;
  }

  const prepared = getPreparedPeakNoOrder(bucket.noTokenId);
  if (config.dryRun) {
    log("peak/no", `attempt ${PEAK_NO_STRATEGY} noTokenId=${bucket.noTokenId} bucket=${formatBucket(bucket)} price≤${PEAK_NO_LIMIT_PRICE} shares=${prepared?.shares ?? "?"} [DRY RUN]`);
    return;
  }
  const t0 = performance.now();
  const fallbackShares = peakShares(PEAK_NO_LIMIT_PRICE, config);
  const order = prepared?.limitOrder ?? await clob.createOrder(
    { tokenID: bucket.noTokenId, price: PEAK_NO_LIMIT_PRICE, size: fallbackShares, side: Side.BUY },
    { tickSize: "0.01", negRisk: bucket.negRisk },
  );
  const submit = clob.postOrder(order, OrderType.FAK);
  const elapsedUs = Math.round((performance.now() - t0) * 1000);
  const sharesDisplay = prepared?.shares ?? fallbackShares;
  log("peak/no", `attempt ${PEAK_NO_STRATEGY} noTokenId=${bucket.noTokenId} bucket=${formatBucket(bucket)} price≤${PEAK_NO_LIMIT_PRICE} shares=${sharesDisplay}${prepared ? " prepared=true" : ""} hotPath=${elapsedUs}µs`);
  const resp = await submit;
  logPeakResult("peak/no", resp, bucket.noTokenId, bucket);
  if (String(resp?.status ?? "") === "matched") {
    bucket.bought = true;
    recordExecutedTrade({ bucket, strategy: PEAK_NO, side: "NO", tokenId: bucket.noTokenId, price: PEAK_NO_LIMIT_PRICE, shares: sharesDisplay });
  }
}

function markMatchedNo(bucket: BucketState, price: number, shares: number | undefined): void {
  bucket.bought = true;
  recordExecutedTrade({ bucket, strategy: CONTESTED_NO, side: "NO", tokenId: bucket.noTokenId, price, shares });
}

function logPeakResult(tag: string, resp: any, tokenId: string, bucket: BucketState): void {
  const status = String(resp?.status ?? "unknown");
  const errDetail: string = resp?.errorMsg || resp?.error || "";
  const strategy = tag === "peak/yes" ? PEAK_YES_STRATEGY : PEAK_NO_STRATEGY;
  log(tag, `result=${status}${errDetail ? ` msg="${errDetail}"` : ""} ${strategy} tokenId=${tokenId} bucket=${formatBucket(bucket)}`);
}

function logResult(tag: string, resp: any, bucket: BucketState): void {
  const status = String(resp?.status ?? "unknown");
  const errDetail: string = resp?.errorMsg || resp?.error || "";
  log(tag, `result=${status}${errDetail ? ` msg="${errDetail}"` : ""} ${CONTESTED_NO_STRATEGY} tokenId=${bucket.noTokenId} bucket=${formatBucket(bucket)}`);
  // Book was empty at execution time — another bot swept it; no point retrying.
  if (status === "400" && errDetail.includes("no orders found")) bucket.attempted = true;
}
