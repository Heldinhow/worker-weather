import { type ClobClient, type OrderSummary } from "@polymarket/clob-client-v2";
import { log } from "./logger.ts";

interface CachedBook {
  asks: OrderSummary[];
  fetchedAt: number;
}

const cache = new Map<string, CachedBook>();
interface StreamState {
  ws: WebSocket | null;
  reconnects: number;
  _connect: (() => void) | null;
}
const streams = new Map<string, StreamState>();

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
  const out: OrderSummary[] = [];
  for (let i = 0; i < orders.length; i++) {
    const raw = orders[i] as { price?: unknown; size?: unknown };
    const price = String(raw.price ?? "");
    const size = String(raw.size ?? "");
    if (Number.isFinite(Number(price)) && Number.isFinite(Number(size))) {
      // Insert in sorted position to avoid full-array sort later
      const p = Number(price);
      let inserted = false;
      for (let j = 0; j < out.length; j++) {
        if (Number(out[j]!.price) > p) {
          out.splice(j, 0, { price, size });
          inserted = true;
          break;
        }
      }
      if (!inserted) out.push({ price, size });
    }
  }
  return out;
}

function isAskSide(side: unknown): boolean {
  const s = String(side ?? "").toUpperCase();
  return s === "SELL" || s === "ASK";
}

function applyAskChange(tokenId: string, price: string, size: string): void {
  const entry = cache.get(tokenId);
  const next = entry ? entry.asks : [];

  // Remove existing level with same price (in-place)
  for (let i = next.length - 1; i >= 0; i--) {
    if (next[i]!.price === price) {
      next.splice(i, 1);
    }
  }

  if (Number(size) > 0) {
    const p = Number(price);
    let inserted = false;
    for (let i = 0; i < next.length; i++) {
      if (Number(next[i]!.price) > p) {
        next.splice(i, 0, { price, size });
        inserted = true;
        break;
      }
    }
    if (!inserted) next.push({ price, size });
  }

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

export function isBookStreamConnected(key: string): boolean {
  const state = streams.get(key);
  return state?.ws?.readyState === WebSocket.OPEN;
}

export function forceReconnectBookStream(key: string): void {
  const state = streams.get(key);
  if (!state) return;
  if (state.ws) {
    state.ws.close();
  }
  state.reconnects = 0;
  // onclose handler will call connect after 1s (2^0 * 1000), but we want it sooner.
  // We'll schedule connect explicitly after a short delay to let the old socket cleanup.
  setTimeout(() => {
    if (!state.ws && state._connect) state._connect();
  }, 200);
}

export function startBookStream(tokenIds: string[], label: string): void {
  const ids = [...new Set(tokenIds)].sort();
  if (ids.length === 0) return;

  const key = ids.join(",");
  if (streams.has(key)) return;

  const state = { ws: null as WebSocket | null, reconnects: 0, _connect: null as (() => void) | null };
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
        const text = typeof event.data === "string" ? event.data : String(event.data);
        applyMarketMessage(JSON.parse(text));
      } catch {
        // Ignore malformed websocket frames; REST refresh remains the fallback.
      }
    };

    ws.onclose = () => {
      if (state.ws !== ws) return;
      state.ws = null;
      const delay = Math.min(1_000 * 2 ** state.reconnects, 5_000);
      state.reconnects += 1;
      setTimeout(connect, delay);
    };

    ws.onerror = () => {
      ws.close();
    };
  };

  state._connect = connect;
  connect();
  log(`book-ws/${label}`, `subscribed tokens=${ids.length}`);
}
