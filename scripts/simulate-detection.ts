// Usage: bun scripts/simulate-detection.ts --icao SBGR --obs 19.5 --initial-max 17.0 [--date 2026-06-03]
//
// Fetches real buckets from Polymarket Gamma API (today by default, or --date YYYY-MM-DD),
// then fires handleObs with a synthetic observation so the full detection → trade path
// runs against the live CLOB. Safe to run with no wallet balance or on a zeroed-out
// bucket — postOrder will attempt and log the CLOB error gracefully.

import { loadConfig } from "../src/config.ts";
import { initClobClient } from "../src/clob.ts";
import { handleObs } from "../src/hot-window.ts";
import type { BucketState } from "../src/types.ts";

const MONTHS = [
  "january", "february", "march", "april", "may", "june",
  "july", "august", "september", "october", "november", "december",
];

function buildSlug(citySlug: string, date?: string): string {
  let d: Date;
  if (date) {
    const [y, m, day] = date.split("-").map(Number);
    d = new Date(Date.UTC(y!, m! - 1, day!));
  } else {
    d = new Date(Date.now() - 3 * 3600_000); // BRT today
  }
  const month = MONTHS[d.getUTCMonth()]!;
  const day = d.getUTCDate();
  const year = d.getUTCFullYear();
  return `highest-temperature-in-${citySlug}-on-${month}-${day}-${year}`;
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

async function fetchBuckets(citySlug: string, date?: string): Promise<BucketState[]> {
  const slug = buildSlug(citySlug, date);
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

  buckets.sort((a, b) => a.tempC - b.tempC);
  return buckets;
}

function parseArgs(): { icao: string; obs: number; initialMax: number; date?: string } {
  const args = process.argv.slice(2);
  const get = (flag: string): string | undefined => {
    const i = args.indexOf(flag);
    return i !== -1 ? args[i + 1] : undefined;
  };

  const icao = get("--icao");
  const obsStr = get("--obs");
  const initialMaxStr = get("--initial-max");
  const date = get("--date");

  if (!icao || !obsStr || !initialMaxStr) {
    console.error("Usage: bun scripts/simulate-detection.ts --icao SBGR --obs 19.5 --initial-max 17.0 [--date 2026-06-03]");
    process.exit(1);
  }

  return { icao, obs: parseFloat(obsStr), initialMax: parseFloat(initialMaxStr), date };
}

const { icao, obs, initialMax, date } = parseArgs();
const config = loadConfig();
const city = config.cities.find(c => c.icao === icao);

if (!city) {
  console.error(`[sim] ICAO ${icao} not found in CITIES config. Available: ${config.cities.map(c => c.icao).join(", ")}`);
  process.exit(1);
}

console.log(`[sim] fetching buckets for ${city.slug} (${icao}) date=${date ?? "today"}...`);
const buckets = await fetchBuckets(city.slug, date);
const exactBuckets = buckets.filter(b => b.type === "exact");
console.log(`[sim] ${buckets.length} buckets loaded. exact: ${exactBuckets.map(b => `${b.tempC}°C`).join(", ")}`);

const clob = await initClobClient(config);
const bucketMap = new Map(buckets.map(b => [b.noTokenId, b]));
const observedMaxRef = { value: initialMax };

const triggered = exactBuckets.filter(b => Math.floor(obs) > b.tempC);
console.log(`[sim] obs=${obs}°C  initial-max=${initialMax}°C  floor(obs)=${Math.floor(obs)}`);
console.log(`[sim] expected triggers: ${triggered.length > 0 ? triggered.map(b => `${b.tempC}°C`).join(", ") : "none"}`);
console.log(`[sim] firing handleObs...\n`);

handleObs(
  { tempC: obs, observedAtUtcMs: Date.now() },
  `sim/${icao}`,
  observedMaxRef,
  bucketMap,
  buckets,
  clob,
  config,
);

// postOrder is async and fire-and-forget inside handleObs — wait for it to settle
await Bun.sleep(10_000);
