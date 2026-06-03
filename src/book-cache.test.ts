import { describe, expect, test } from "bun:test";
import { applyMarketMessage, getCachedAsks } from "./book-cache.ts";

describe("applyMarketMessage", () => {
  test("stores book snapshots and applies sell-side price changes", () => {
    applyMarketMessage({
      event_type: "book",
      asset_id: "token-1",
      asks: [{ price: "0.91", size: "2" }],
    });

    expect(getCachedAsks("token-1")).toEqual([{ price: "0.91", size: "2" }]);

    applyMarketMessage({
      event_type: "price_change",
      changes: [
        { asset_id: "token-1", side: "SELL", price: "0.91", size: "0" },
        { asset_id: "token-1", side: "SELL", price: "0.93", size: "1.5" },
      ],
    });

    expect(getCachedAsks("token-1")).toEqual([{ price: "0.93", size: "1.5" }]);
  });
});
