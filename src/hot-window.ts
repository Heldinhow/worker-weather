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

const DAY_S = 86_400;

function brtHourMin(nowMs: number): { h: number; m: number } {
  const brtS = Math.floor(nowMs / 1000) - 3 * 3600;
  const dayS = ((brtS % DAY_S) + DAY_S) % DAY_S;
  return { h: Math.floor(dayS / 3600), m: Math.floor((dayS % 3600) / 60) };
}

export function isHotWindow(
  now: Date,
  targetHourBrt: number,
  windowStart: number,
  windowEnd: number,
): boolean {
  return isHotWindowMs(now.getTime(), targetHourBrt, windowStart, windowEnd);
}

function isHotWindowMs(
  nowMs: number,
  targetHourBrt: number,
  windowStart: number,
  windowEnd: number,
): boolean {
  const { h, m } = brtHourMin(nowMs);
  const prevH = (targetHourBrt - 1 + 24) % 24;
  return (h === prevH && m >= windowStart) || (h === targetHourBrt && m <= windowEnd);
}

function getBrtHour(now: Date): number {
  const brtS = Math.floor(now.getTime() / 1000) - 3 * 3600;
  return Math.floor(((brtS % DAY_S) + DAY_S) % DAY_S / 3600);
}

function getMetarBrtHour(obs: ObservationResult): number {
  const brtS = Math.floor(obs.observedAtUtcMs / 1000) - 3 * 3600;
  return Math.floor(((brtS % DAY_S) + DAY_S) % DAY_S / 3600);
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
  const controllers: AbortController[] = [];

  const stopAll = () => {
    stop = true;
    for (let i = 0; i < controllers.length; i++) controllers[i]!.abort();
  };

  await Promise.all(sources.map(async ({ source, fetch }) => {
    while (!stop && isActive()) {
      const ac = new AbortController();
      controllers.push(ac);
      const timeout = setTimeout(() => ac.abort(), 12_000);

      try {
        const obs = await fetch(ac.signal).catch(() => null);
        if (stop && ac.signal.aborted) break;

        onObservation(obs, source);
        if (obs && shouldStop(obs)) stopAll();
      } finally {
        clearTimeout(timeout);
        const idx = controllers.indexOf(ac);
        if (idx >= 0) controllers.splice(idx, 1);
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
  if (obs.tempC <= ref.value) {
    log(source, `tempC=${obs.tempC} metar=${formatBrt(obs.observedAtUtcMs)}`);
    return;
  }

  const prevValue = ref.value;
  ref.value = obs.tempC;
  evaluateBuckets(obs.tempC, buckets, b => {
    postOrder(clob, b, config);
  });
  const prev = prevValue === -Infinity ? "-∞" : String(prevValue);
  log(source, `tempC=${obs.tempC} metar=${formatBrt(obs.observedAtUtcMs)}`);
  log(source, `observedMax ${prev} → ${obs.tempC}`);
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
    const nowMs = Date.now();
    const activeHour = city.targetHours.find(h => isHotWindowMs(nowMs, h, city.hotWindowStart, city.hotWindowEnd));

    if (activeHour !== undefined) {
      // Ensure book cache is fresh before entering the hot window
      if (!config.dryRun) await Promise.all([
        refreshBooks(clob, exactTokenIds),
        prepareOrders(clob, buckets, config),
      ]);
      log(city.icao, `hot window open targetHour=${activeHour}`);

      const prevHour = (activeHour - 1 + 24) % 24;

      while (isHotWindowMs(Date.now(), activeHour, city.hotWindowStart, city.hotWindowEnd)) {
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
          () => isHotWindowMs(Date.now(), activeHour, city.hotWindowStart, city.hotWindowEnd),
          obs => getMetarBrtHour(obs) !== prevHour,
          (obs, source) => handleObs(obs, source, observedMaxRef, bucketMap, buckets, clob, config),
        );
      }

      log(city.icao, `hot window closed targetHour=${activeHour}`);
      while (isHotWindowMs(Date.now(), activeHour, city.hotWindowStart, city.hotWindowEnd)) {
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
