import { type ClobClient, Side, OrderType, type OrderBookSummary } from "@polymarket/clob-client-v2";
import type { BucketState } from "./types.ts";
import type { Config } from "./config.ts";

export async function postOrder(
  clob: ClobClient,
  bucket: BucketState,
  config: Config,
): Promise<void> {
  try {
    const book: OrderBookSummary = await clob.getOrderBook(bucket.noTokenId);
    const asks = book.asks ?? [];

    let shares = 0;
    let cost = 0;
    let price = 0.99;

    if (asks.length === 0) {
      bucket.attempted = true;
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

    if (shares < config.minShares && !bucket.attempted) {
      return;
    }

    if (cost < 1.0 && config.prod) {
      return;
    }

    const priceRounded = Math.round(price * 100) / 100;
    const sharesRounded = Math.round(shares * 100) / 100;

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

    if (resp?.status === "matched") {
      bucket.bought = true;
    }
  } catch (err) {
    process.stderr.write(`postOrder error for token ${bucket.noTokenId}: ${err}\n`);
  } finally {
    bucket.pendingBuy = false;
  }
}
