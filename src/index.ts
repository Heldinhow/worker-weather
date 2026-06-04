import type { ClobClient } from "@polymarket/clob-client-v2";
import { loadConfig, type CityConfig, type Config } from "./config.ts";
import { initClobClient } from "./clob.ts";
import type { BucketState } from "./types.ts";
import { runHotWindowLoop } from "./hot-window.ts";
import { log, registerCityColor } from "./logger.ts";
import { startDashboard } from "./dashboard.ts";
import { todaySlug } from "./time.ts";
import { startBookStream } from "./book-cache.ts";

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

async function resolveSlugAndMarkets(city: CityConfig): Promise<{ slug: string; buckets: BucketState[] }> {
  const slug = todaySlug(city.slug);
  const resp = await fetch(`https://gamma-api.polymarket.com/events?slug=${slug}`);
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

  return { slug, buckets };
}

async function runCity(city: CityConfig, config: Config, clob: ClobClient, buckets: BucketState[]): Promise<void> {
  while (true) {
    const observedMaxRef = { value: -Infinity };
    const deadline = Date.now() + getMsUntilMidnightBrt();

    await runHotWindowLoop(city, config, clob, buckets, observedMaxRef, deadline);

    await Bun.sleep(2_000);
  }
}

const config = loadConfig();
config.cities.forEach((city, i) => registerCityColor(city.icao, i));
startDashboard(config.cities);

if (config.dryRun) {
  log("boot", "DRY RUN mode — no orders will be posted to the CLOB");
}

const clob = config.dryRun
  ? null as unknown as ClobClient
  : await initClobClient(config);

// Resolve markets for all cities once per day
const cityBuckets = new Map<string, BucketState[]>();
const allExactTokenIds: string[] = [];
for (const city of config.cities) {
  const { buckets } = await resolveSlugAndMarkets(city);
  cityBuckets.set(city.icao, buckets);
  const exact = buckets.filter(b => b.type === "exact").map(b => b.noTokenId);
  allExactTokenIds.push(...exact);
}

// Start a single shared book stream for all cities to reduce WS connection overhead
if (!config.dryRun && allExactTokenIds.length > 0) {
  startBookStream([...new Set(allExactTokenIds)], "all");
  await Bun.sleep(300);
}

await Promise.all(config.cities.map(city => runCity(city, config, clob, cityBuckets.get(city.icao)!)));
