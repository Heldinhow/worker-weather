import type { ClobClient } from "@polymarket/clob-client-v2";
import type { BucketState, ObservationResult } from "./types.ts";
import type { CityConfig, Config } from "./config.ts";
import { fetchNoaa } from "./fetchers/noaa.ts";
import { fetchAviationWeather } from "./fetchers/aviation-weather.ts";
import { evaluateBuckets } from "./evaluator.ts";
import { postOrder } from "./trader.ts";
import { refreshBooks, startBookStream } from "./book-cache.ts";
import { prepareOrders } from "./order-cache.ts";
import { log, formatBrt } from "./logger.ts";

export function isHotWindow(
  now: Date,
  targetHourBrt: number,
  windowStart: number,
  windowEnd: number,
): boolean {
  const brt = new Date(now.getTime() - 3 * 3600_000);
  const h = brt.getUTCHours();
  const m = brt.getUTCMinutes();
  const prevH = (targetHourBrt - 1 + 24) % 24;
  return (h === prevH && m >= windowStart) || (h === targetHourBrt && m <= windowEnd);
}

function getBrtHour(now: Date): number {
  return new Date(now.getTime() - 3 * 3600_000).getUTCHours();
}

function getMetarBrtHour(obs: ObservationResult): number {
  return new Date(obs.observedAtUtcMs - 3 * 3600_000).getUTCHours();
}

export async function processObservationFetches(
  fetches: { source: string; promise: Promise<ObservationResult | null> }[],
  onObservation: (obs: ObservationResult | null, source: string) => void,
): Promise<(ObservationResult | null)[]> {
  return Promise.all(fetches.map(({ source, promise }) => promise.then(obs => {
    onObservation(obs, source);
    return obs;
  })));
}

export async function runHotObservationLoops(
  sources: {
    source: string;
    fetch: (signal: AbortSignal) => Promise<ObservationResult | null>;
  }[],
  isActive: () => boolean,
  shouldStop: (obs: ObservationResult) => boolean,
  onObservation: (obs: ObservationResult | null, source: string) => void,
): Promise<void> {
  let stop = false;
  const controllers = new Set<AbortController>();

  const stopAll = () => {
    stop = true;
    for (const controller of controllers) controller.abort();
  };

  await Promise.all(sources.map(async ({ source, fetch }) => {
    while (!stop && isActive()) {
      const ac = new AbortController();
      controllers.add(ac);
      const timeout = setTimeout(() => ac.abort(), 12_000);

      try {
        const obs = await fetch(ac.signal).catch(() => null);
        if (stop && ac.signal.aborted) break;

        onObservation(obs, source);
        if (obs && shouldStop(obs)) stopAll();
      } finally {
        clearTimeout(timeout);
        controllers.delete(ac);
      }
    }
  }));
}

export function handleObs(
  obs: ObservationResult | null,
  source: string,
  ref: { value: number },
  bucketMap: Map<string, BucketState>,
  buckets: BucketState[],
  clob: ClobClient,
  config: Config,
): void {
  if (!obs) {
    log(source, "no data");
    return;
  }
  log(source, `tempC=${obs.tempC} metar=${formatBrt(new Date(obs.observedAtUtcMs))}`);
  if (obs.tempC <= ref.value) return;
  const prev = ref.value === -Infinity ? "-∞" : String(ref.value);
  log(source, `observedMax ${prev} → ${obs.tempC}`);
  ref.value = obs.tempC;
  evaluateBuckets(ref.value, buckets, tokenId => {
    const b = bucketMap.get(tokenId);
    if (b) postOrder(clob, b, config);
  });
}

export async function runHotWindowLoop(
  city: CityConfig,
  config: Config,
  clob: ClobClient,
  buckets: BucketState[],
  observedMaxRef: { value: number },
  deadline: number,
): Promise<void> {
  // Pre-compute once — used on every iteration
  const bucketMap = new Map(buckets.map(b => [b.noTokenId, b]));
  const exactTokenIds = buckets.filter(b => b.type === "exact").map(b => b.noTokenId);

  if (!config.dryRun) {
    startBookStream(exactTokenIds, city.icao);
    void prepareOrders(clob, buckets, config);
  }

  while (Date.now() < deadline) {
    const now = new Date();
    const activeHour = city.targetHours.find(h => isHotWindow(now, h, city.hotWindowStart, city.hotWindowEnd));

    if (activeHour !== undefined) {
      // Ensure book cache is fresh before entering the hot window
      if (!config.dryRun) await Promise.all([
        refreshBooks(clob, exactTokenIds),
        prepareOrders(clob, buckets, config),
      ]);
      log(city.icao, `hot window open targetHour=${activeHour}`);

      const prevHour = (activeHour - 1 + 24) % 24;

      while (isHotWindow(new Date(), activeHour, city.hotWindowStart, city.hotWindowEnd)) {
        await runHotObservationLoops(
          [
            {
              source: `noaa/${city.icao}`,
              fetch: signal => fetchNoaa(city.icao, signal),
            },
            {
              source: `aw/${city.icao}`,
              fetch: signal => fetchAviationWeather(city.icao, signal),
            },
          ],
          () => isHotWindow(new Date(), activeHour, city.hotWindowStart, city.hotWindowEnd),
          obs => getMetarBrtHour(obs) !== prevHour,
          (obs, source) => handleObs(obs, source, observedMaxRef, bucketMap, buckets, clob, config),
        );
      }

      log(city.icao, `hot window closed targetHour=${activeHour}`);
      while (isHotWindow(new Date(), activeHour, city.hotWindowStart, city.hotWindowEnd)) {
        await Bun.sleep(200);
      }
    } else {
      // Warm poll: METAR fetches + book cache refresh run concurrently
      await Promise.all([
        processObservationFetches(
          [
            { source: `noaa/${city.icao}`, promise: fetchNoaa(city.icao) },
            { source: `aw/${city.icao}`, promise: fetchAviationWeather(city.icao) },
          ],
          (obs, source) => handleObs(obs, source, observedMaxRef, bucketMap, buckets, clob, config),
        ),
        config.dryRun ? Promise.resolve() : refreshBooks(clob, exactTokenIds),
      ]);

      const brtH = getBrtHour(new Date());
      const minH = Math.min(...city.targetHours);
      const maxH = Math.max(...city.targetHours);
      const sleepMs = brtH >= minH && brtH <= maxH ? 30_000 : 600_000;
      await Bun.sleep(sleepMs);
    }
  }
}
