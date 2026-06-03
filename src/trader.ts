import { type ClobClient, Side, OrderType } from "@polymarket/clob-client-v2";
import type { BucketState } from "./types.ts";
import type { Config } from "./config.ts";
import { getCachedAsks } from "./book-cache.ts";
import { log } from "./logger.ts";

function gcd(a: number, b: number): number {
  while (b) { [a, b] = [b, a % b]; }
  return a;
}

// FAK orders require price * shares to have ≤ 2 decimal places.
// For price p = a/100, the minimum valid share step is 1/gcd(a,100).
function snapShares(shares: number, price: number): number {
  const a = Math.round(price * 100);
  const step = 1 / gcd(a, 100);
  return Math.floor(shares / step) * step;
}

// Max valid CLOB price for a binary market — used as limit ceiling when book is unknown.
const LIMIT_PRICE = 0.99;

export async function postOrder(
  clob: ClobClient,
  bucket: BucketState,
  config: Config,
): Promise<void> {
  try {
    const cachedAsks = getCachedAsks(bucket.noTokenId);

    if (cachedAsks !== null && cachedAsks.length === 0) {
      bucket.attempted = true;
      log("trader", `skip tokenId=${bucket.noTokenId} tempC=${bucket.tempC} reason=empty-book`);
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

    log("trader", `attempt tokenId=${bucket.noTokenId} tempC=${bucket.tempC} price≤${LIMIT_PRICE} shares=${sharesRounded}${config.dryRun ? " [DRY RUN]" : ""}`);

    if (config.dryRun) {
      bucket.bought = true;
      return;
    }

    const order = await clob.createOrder(
      { tokenID: bucket.noTokenId, price: LIMIT_PRICE, size: sharesRounded, side: Side.BUY },
      { tickSize: "0.01", negRisk: bucket.negRisk },
    );

    const resp = await clob.postOrder(order, OrderType.FAK);
    logResult("trader", resp, bucket);
    if (String(resp?.status ?? "") === "matched") bucket.bought = true;

  } catch (err) {
    log("trader", `error tokenId=${bucket.noTokenId} tempC=${bucket.tempC} err=${err}`);
  } finally {
    bucket.pendingBuy = false;
  }
}

async function postBlindExperiment(
  clob: ClobClient,
  bucket: BucketState,
  config: Config,
): Promise<void> {
  const shares = Math.max(config.minShares, snapShares(config.maxStake / LIMIT_PRICE, LIMIT_PRICE));

  log("trader", `blind attempt tokenId=${bucket.noTokenId} tempC=${bucket.tempC} limit-fak price=${LIMIT_PRICE} shares=${shares} market-fak amount=${config.maxStake}${config.dryRun ? " [DRY RUN]" : ""}`);

  if (config.dryRun) {
    bucket.bought = true;
    return;
  }

  const [limitResult, marketResult] = await Promise.allSettled([
    (async () => {
      const order = await clob.createOrder(
        { tokenID: bucket.noTokenId, price: LIMIT_PRICE, size: shares, side: Side.BUY },
        { tickSize: "0.01", negRisk: bucket.negRisk },
      );
      return clob.postOrder(order, OrderType.FAK);
    })(),
    clob.createAndPostMarketOrder(
      { tokenID: bucket.noTokenId, amount: config.maxStake, side: Side.BUY },
      { tickSize: "0.01", negRisk: bucket.negRisk },
      OrderType.FAK,
    ),
  ]);

  for (const [label, result] of [["limit-fak", limitResult], ["market-fak", marketResult]] as const) {
    if (result.status === "fulfilled") {
      logResult(`trader/${label}`, result.value, bucket);
      if (String(result.value?.status ?? "") === "matched") bucket.bought = true;
    } else {
      log(`trader/${label}`, `error tokenId=${bucket.noTokenId} tempC=${bucket.tempC} err=${result.reason}`);
    }
  }
}

function logResult(tag: string, resp: any, bucket: BucketState): void {
  const status = String(resp?.status ?? "unknown");
  const errDetail: string = resp?.errorMsg || resp?.error || "";
  log(tag, `result=${status}${errDetail ? ` msg="${errDetail}"` : ""} tokenId=${bucket.noTokenId} tempC=${bucket.tempC}`);
}
