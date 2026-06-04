import type { ClobClient } from "@polymarket/clob-client-v2";
import { loadConfig, type CityConfig, type Config } from "./config.ts";
import { initClobClient } from "./clob.ts";
import type { BucketState } from "./types.ts";
import { runHotWindowLoop } from "./hot-window.ts";
import { log, registerCityColor } from "./logger.ts";
import { startDashboard } from "./dashboard.ts";
import { todaySlug } from "./time.ts";

function getMsUntilMidnightBrt(): number {
  const now = Date.now();
  const brtMs = now - 3 * 3600_000;
  const dayMs = 86_400_000;
  const tomorrowStartBrt = Math.floor(brtMs / dayMs) * dayMs + dayMs;
  return tomorrowStartBrt - brtMs + 3 * 3600_000;
}

interface GammaMarket {
  question: string;
  clobTokenIds: string;
  conditionId: string;
}

interface GammaEvent {
  negRisk: boolean;
  markets: GammaMarket[];
}

const slugCache = new Map<string, { slug: string; buckets: BucketState[] }>();

async function resolveSlugAndMarkets(city: CityConfig): Promise<{ slug: string; buckets: BucketState[] }> {
  const slug = todaySlug(city.slug);
  const cached = slugCache.get(slug);
  if (cached) return cached;

  const resp = await fetch(`https://gamma-api.polymarket.com/events?slug=${slug}`, { keepalive: true });
  const events = await resp.json() as GammaEvent[];
  const event = events[0]!;

  const buckets: BucketState[] = event.markets.map(m => {
    const [, noId] = JSON.parse(m.clobTokenIds) as [string, string];
    const tempMatch = m.question.match(/(\d+)°C/);
    const tempC = tempMatch ? parseInt(tempMatch[1]!, 10) : 0;
    const lq = m.question.toLowerCase();
    const type: "exact" | "below" | "above" =
      lq.includes("or below") ? "below" :
      lq.includes("or higher") ? "above" : "exact";

    return {
      tempC,
      type,
      noTokenId: noId!,
      conditionId: m.conditionId,
      negRisk: event.negRisk,
      bought: false,
      attempted: false,
      pendingBuy: false,
    };
  });

  // Sort ascending by tempC so evaluateBuckets can break early
  buckets.sort((a, b) => a.tempC - b.tempC);

  const result = { slug, buckets };
  slugCache.set(slug, result);
  return result;
}

async function runCity(city: CityConfig, config: Config, clob: ClobClient, initialBuckets: BucketState[]): Promise<void> {
  let buckets = initialBuckets;
  while (true) {
    const observedMaxRef = { value: -Infinity };
    const deadline = Date.now() + getMsUntilMidnightBrt();

    await runHotWindowLoop(city, config, clob, buckets, observedMaxRef, deadline);

    // Re-resolve markets in case the day changed while we were inside the loop
    const resolved = await resolveSlugAndMarkets(city);
    buckets = resolved.buckets;
    await Bun.sleep(100);
  }
}

const config = loadConfig();
config.cities.forEach((city, i) => registerCityColor(city.icao, i));
startDashboard(config.cities);

if (config.dryRun) {
  log("boot", "DRY RUN mode — no orders will be posted to the CLOB");
}

// Parallelize CLOB init with market resolution to reduce cold-start time
const clobPromise = config.dryRun
  ? Promise.resolve(null as unknown as ClobClient)
  : initClobClient(config);
const marketsPromise = Promise.all(config.cities.map(city => resolveSlugAndMarkets(city)));

const [clob, markets] = await Promise.all([clobPromise, marketsPromise]);

await Promise.all(config.cities.map((city, i) => runCity(city, config, clob, markets[i]!.buckets)));
