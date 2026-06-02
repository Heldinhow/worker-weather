import { type ClobClient, type OrderSummary } from "@polymarket/clob-client-v2";

interface CachedBook {
  asks: OrderSummary[];
  fetchedAt: number;
}

const cache = new Map<string, CachedBook>();

// 90s TTL — warm polls run every 30s so cache is always fresh in normal operation
const STALE_MS = 90_000;

export function getCachedAsks(tokenId: string): OrderSummary[] | null {
  const entry = cache.get(tokenId);
  if (!entry) return null;
  if (Date.now() - entry.fetchedAt > STALE_MS) return null;
  return entry.asks;
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
