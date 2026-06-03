import { type ClobClient, type OrderSummary } from "@polymarket/clob-client-v2";
import { log } from "./logger.ts";

interface CachedBook {
  asks: OrderSummary[];
  fetchedAt: number;
}

const cache = new Map<string, CachedBook>();
const streams = new Map<string, { ws: WebSocket | null; reconnects: number }>();

// 90s TTL — warm polls run every 30s so cache is always fresh in normal operation
const STALE_MS = 90_000;
const MARKET_WS_URL = "wss://ws-subscriptions-clob.polymarket.com/ws/market";

export function getCachedAsks(tokenId: string): OrderSummary[] | null {
  const entry = cache.get(tokenId);
  if (!entry) return null;
  if (Date.now() - entry.fetchedAt > STALE_MS) return null;
  return entry.asks;
}

// Hot-path variant: skips stale check. Callers must ensure cache is fresh.
export function getCachedAsksFast(tokenId: string): OrderSummary[] | null {
  const entry = cache.get(tokenId);
  return entry ? entry.asks : null;
}

export async function refreshBooks(clob: ClobClient, tokenIds: string[]): Promise<void> {
  await Promise.all(
    tokenIds.map(async id => {
      try {
        const book = await clob.getOrderBook(id);
        cache.set(id, { asks: book.asks ?? [], fetchedAt: Date.now() });
      } catch { /* best-effort */ }
    }),
  );
}

function normalizeOrders(orders: unknown): OrderSummary[] {
  if (!Array.isArray(orders)) return [];
  return orders
    .map(order => {
      const raw = order as { price?: unknown; size?: unknown };
      return {
        price: String(raw.price ?? ""),
        size: String(raw.size ?? ""),
      };
    })
    .filter(order => Number.isFinite(Number(order.price)) && Number.isFinite(Number(order.size)))
    .sort((a, b) => Number(a.price) - Number(b.price));
}

function isAskSide(side: unknown): boolean {
  const s = String(side ?? "").toUpperCase();
  return s === "SELL" || s === "ASK";
}

function applyAskChange(tokenId: string, price: string, size: string): void {
  const existing = cache.get(tokenId)?.asks ?? [];
  const next = existing.filter(ask => ask.price !== price);

  if (Number(size) > 0) {
    next.push({ price, size });
  }

  next.sort((a, b) => Number(a.price) - Number(b.price));
  cache.set(tokenId, { asks: next, fetchedAt: Date.now() });
}

export function applyMarketMessage(raw: unknown): void {
  const messages = Array.isArray(raw) ? raw : [raw];

  for (const message of messages) {
    const msg = message as {
      event_type?: string;
      asset_id?: string;
      asks?: unknown;
      changes?: unknown;
    };

    if (msg.event_type === "book" && msg.asset_id) {
      cache.set(msg.asset_id, { asks: normalizeOrders(msg.asks), fetchedAt: Date.now() });
      continue;
    }

    if (msg.event_type !== "price_change" || !Array.isArray(msg.changes)) continue;

    for (const rawChange of msg.changes) {
      const change = rawChange as {
        asset_id?: string;
        side?: unknown;
        price?: unknown;
        size?: unknown;
      };
      if (!change.asset_id || !isAskSide(change.side)) continue;

      const price = String(change.price ?? "");
      const size = String(change.size ?? "");
      if (!Number.isFinite(Number(price)) || !Number.isFinite(Number(size))) continue;
      applyAskChange(change.asset_id, price, size);
    }
  }
}

export function startBookStream(tokenIds: string[], label: string): void {
  const ids = [...new Set(tokenIds)].sort();
  if (ids.length === 0) return;

  const key = ids.join(",");
  if (streams.has(key)) return;

  const state = { ws: null as WebSocket | null, reconnects: 0 };
  streams.set(key, state);

  const connect = () => {
    const ws = new WebSocket(MARKET_WS_URL);
    state.ws = ws;

    ws.onopen = () => {
      state.reconnects = 0;
      ws.send(JSON.stringify({
        assets_ids: ids,
        type: "market",
        custom_feature_enabled: true,
      }));
    };

    ws.onmessage = event => {
      try {
        applyMarketMessage(JSON.parse(String(event.data)));
      } catch {
        // Ignore malformed websocket frames; REST refresh remains the fallback.
      }
    };

    ws.onclose = () => {
      if (state.ws !== ws) return;
      state.ws = null;
      const delay = Math.min(1_000 * 2 ** state.reconnects, 30_000);
      state.reconnects += 1;
      setTimeout(connect, delay);
    };

    ws.onerror = () => {
      ws.close();
    };
  };

  connect();
  log(`book-ws/${label}`, `subscribed tokens=${ids.length}`);
}
