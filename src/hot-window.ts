import type { ClobClient } from "@polymarket/clob-client-v2";
import type { BucketState, ObservationResult } from "./types.ts";
import type { CityConfig, Config } from "./config.ts";
import { fetchNoaa } from "./fetchers/noaa.ts";
import { fetchAviationWeather } from "./fetchers/aviation-weather.ts";
import { evaluateBuckets } from "./evaluator.ts";
import { postOrder } from "./trader.ts";
import { refreshBooks, startBookStream } from "./book-cache.ts";
import { prepareOrders } from "./order-cache.ts";
import { log } from "./logger.ts";
import { formatBrt, isHotWindow, isHotWindowMs } from "./time.ts";
import { updateCity } from "./dashboard.ts";

const DAY_S = 86_400;

function getBrtHour(now: Date): number {
  const brtS = Math.floor(now.getTime() / 1000) - 3 * 3600;
  return Math.floor(((brtS % DAY_S) + DAY_S) % DAY_S / 3600);
}

// Seconds since midnight BRT
function brtSecondsSinceMidnight(ms: number): number {
  const brtS = Math.floor(ms / 1000) - 3 * 3600;
  const dayS = ((brtS % DAY_S) + DAY_S) % DAY_S;
  return dayS;
}

// How many ms until the next hot window opens for this city?
// Returns 0 if currently inside a hot window.
function msUntilNextHotWindow(nowMs: number, city: CityConfig): number {
  const ssm = brtSecondsSinceMidnight(nowMs);
  const currentH = Math.floor(ssm / 3600);
  const currentM = Math.floor((ssm % 3600) / 60);

  // Check each target hour to find the next window opening
  for (const target of city.targetHours) {
    const prevH = (target - 1 + 24) % 24;
    const openS = prevH * 3600 + city.hotWindowStart * 60;
    const closeS = target * 3600 + city.hotWindowEnd * 60;

    // If currently inside this window, return 0
    const currentS = currentH * 3600 + currentM * 60 + (nowMs % 60000) / 1000;
    if (currentS >= openS && currentS <= closeS) return 0;

    if (currentS < openS) {
      return (openS - currentS) * 1000;
    }
  }

  // All windows for today have passed — next is first target hour tomorrow
  const firstTarget = city.targetHours[0]!;
  const prevH = (firstTarget - 1 + 24) % 24;
  const openS = prevH * 3600 + city.hotWindowStart * 60;
  const tomorrowOpenS = openS + DAY_S;
  const currentS = currentH * 3600 + currentM * 60 + (nowMs % 60000) / 1000;
  return (tomorrowOpenS - currentS) * 1000;
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
      const timeout = setTimeout(() => ac.abort(), 5_000);

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
  icao: string,
  ref: { value: number },
  bucketMap: Map<string, BucketState>,
  buckets: BucketState[],
  clob: ClobClient,
  config: Config,
): void {
  if (!obs) return;
  if (obs.tempC <= ref.value) return;

  const prevValue = ref.value;
  ref.value = obs.tempC;
  const detectedAtMs = Date.now();

  updateCity(icao, { observedMax: obs.tempC, metarTimestampMs: obs.observedAtUtcMs, detectedAtMs });

  evaluateBuckets(obs.tempC, buckets, b => {
    postOrder(clob, b, config);
  });

  const prev = prevValue === -Infinity ? "-∞" : String(prevValue);
  log(source, `observedMax ${prev} → ${obs.tempC} metar=${formatBrt(obs.observedAtUtcMs)}`);
}

export async function runHotWindowLoop(
  city: CityConfig,
  config: Config,
  clob: ClobClient,
  buckets: BucketState[],
  observedMaxRef: { value: number },
  deadline: number,
): Promise<void> {
  const bucketMap = new Map(buckets.map(b => [b.noTokenId, b]));
  const exactTokenIds = buckets.filter(b => b.type === "exact").map(b => b.noTokenId);

  if (!config.dryRun) {
    startBookStream(exactTokenIds, city.icao);
    void prepareOrders(clob, buckets, config);
    // Warm-up HTTP connections so the first fetch inside the hot window
    // reuses an already-established TCP/TLS handshake.
    void fetchNoaa(city.icao).catch(() => undefined);
    void fetchAviationWeather(city.icao).catch(() => undefined);
  }

  while (Date.now() < deadline) {
    const nowMs = Date.now();
    const activeHour = city.targetHours.find(h => isHotWindowMs(nowMs, h, city.hotWindowStart, city.hotWindowEnd));

    if (activeHour !== undefined) {
      if (!config.dryRun) {
        void refreshBooks(clob, exactTokenIds);
        void prepareOrders(clob, buckets, config);
      }
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
          (obs, source) => handleObs(obs, source, city.icao, observedMaxRef, bucketMap, buckets, clob, config),
        );
      }

      log(city.icao, `hot window closed targetHour=${activeHour}`);
      while (isHotWindowMs(Date.now(), activeHour, city.hotWindowStart, city.hotWindowEnd)) {
        await Bun.sleep(200);
      }
    } else {
      await Promise.all([
        processObservationFetches(
          [
            { source: `noaa/${city.icao}`, promise: fetchNoaa(city.icao) },
            { source: `aw/${city.icao}`, promise: fetchAviationWeather(city.icao) },
          ],
          (obs, source) => handleObs(obs, source, city.icao, observedMaxRef, bucketMap, buckets, clob, config),
        ),
        config.dryRun ? Promise.resolve() : refreshBooks(clob, exactTokenIds),
      ]);

      const preciseSleep = msUntilNextHotWindow(Date.now(), city);
      // If a hot window is imminent (< 2 min), wake up precisely then;
      // otherwise use the standard long sleep to avoid busy-waiting.
      const sleepMs = preciseSleep > 0 && preciseSleep < 120_000 ? preciseSleep : 600_000;
      await Bun.sleep(sleepMs);
    }
  }
}
