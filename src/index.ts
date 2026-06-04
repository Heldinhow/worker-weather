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
  const brt = new Date(now - 3 * 3600_000);
  const nextMidnightUtc = Date.UTC(
    brt.getUTCFullYear(),
    brt.getUTCMonth(),
    brt.getUTCDate() + 1,
    3, 0, 0, 0,
  );
  return nextMidnightUtc - now;
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

async function runCity(city: CityConfig, config: Config, clob: ClobClient): Promise<void> {
  while (true) {
    const { buckets } = await resolveSlugAndMarkets(city);
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

await Promise.all(config.cities.map(city => runCity(city, config, clob)));
