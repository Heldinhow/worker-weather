/**
 * Latency benchmark for all external endpoints the bot depends on.
 * Run on each machine and compare results.
 *
 *   bun run scripts/latency-check.ts
 */

const UA = "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36";
const RUNS = 10;
const GAP_MS = 300; // pause between requests to avoid rate-limiting

const ENDPOINTS = [
  {
    label: "noaa/SBGR",
    url: "https://tgftp.nws.noaa.gov/data/observations/metar/stations/SBGR.TXT",
  },
  {
    label: "aviation-weather/SBGR",
    url: "https://aviationweather.gov/api/data/metar?ids=SBGR&format=json&metar=true&hours=3",
  },
  {
    label: "polymarket-gamma",
    url: "https://gamma-api.polymarket.com/events?limit=1",
  },
  {
    label: "polymarket-clob",
    url: "https://clob.polymarket.com",
  },
];

function percentile(sorted: number[], p: number): number {
  return sorted[Math.min(Math.floor(sorted.length * p), sorted.length - 1)]!;
}

function fmt(ms: number): string {
  return ms.toFixed(0).padStart(5) + "ms";
}

async function bench(label: string, url: string): Promise<void> {
  // warm-up — establishes TLS session, excluded from stats
  await fetch(url, {
    headers: { "Cache-Control": "no-cache", "User-Agent": UA },
  }).catch(() => {});

  const times: number[] = [];
  const errors: string[] = [];

  for (let i = 0; i < RUNS; i++) {
    const t0 = performance.now();
    try {
      const resp = await fetch(url, {
        headers: { "Cache-Control": "no-cache", "User-Agent": UA },
        signal: AbortSignal.timeout(15_000),
      });
      await resp.arrayBuffer(); // consume full body
      times.push(performance.now() - t0);
    } catch (err) {
      errors.push(String(err));
    }
    await Bun.sleep(GAP_MS);
  }

  const tag = `  ${label}`;
  if (times.length === 0) {
    console.log(`${tag}: FAIL (${errors[0]})`);
    return;
  }

  const sorted = [...times].sort((a, b) => a - b);
  const min    = sorted[0]!;
  const median = percentile(sorted, 0.50);
  const p90    = percentile(sorted, 0.90);
  const p95    = percentile(sorted, 0.95);
  const max    = sorted[sorted.length - 1]!;
  const mean   = times.reduce((a, b) => a + b, 0) / times.length;

  const ok = `${times.length}/${RUNS}`;
  console.log(`\n  ${label}  (${ok} ok)`);
  console.log(`    min=${fmt(min)}  median=${fmt(median)}  mean=${fmt(mean)}  p90=${fmt(p90)}  p95=${fmt(p95)}  max=${fmt(max)}`);
}

const hostname = Bun.env.HOSTNAME ?? (await Bun.spawn(["hostname"]).stdout
  .getReader().read().then(r => new TextDecoder().decode(r.value).trim()));

console.log(`\n========================================`);
console.log(` Latency check — ${hostname}`);
console.log(` ${RUNS} requests per endpoint + 1 warm-up`);
console.log(`========================================`);

for (const ep of ENDPOINTS) {
  await bench(ep.label, ep.url);
}

console.log(`\n========================================\n`);
