import type { ClobClient } from "@polymarket/clob-client-v2";
import type { BucketState, ObservationResult } from "./types.ts";
import type { CityConfig, Config } from "./config.ts";
import { fetchNoaa } from "./fetchers/noaa.ts";
import { fetchAviationWeather } from "./fetchers/aviation-weather.ts";
import { evaluateBuckets, evaluatePeakDrop, evaluatePeakAtHour } from "./evaluator.ts";
import { postOrder, postPeakOrders } from "./trader.ts";
import { refreshBooks, startBookStream, isBookStreamConnected, forceReconnectBookStream } from "./book-cache.ts";
import { prepareOrders } from "./order-cache.ts";
import { log } from "./logger.ts";
import { formatBucket } from "./markets.ts";
import { formatBrt, getLocalHour, getLocalSecondsSinceMidnight, isHotWindowMs } from "./time.ts";
import { updateCity } from "./dashboard.ts";

const DAY_S = 86_400;

// How many ms until the next hot window opens for this city?
// Returns 0 if currently inside a hot window.
export function msUntilNextHotWindow(nowMs: number, city: CityConfig): number {
  const currentS = getLocalSecondsSinceMidnight(nowMs, city.timezone);

  // Check each target hour to find the next window opening
  for (const target of city.targetHours) {
    const prevH = (target - 1 + 24) % 24;
    const openS = prevH * 3600 + city.hotWindowStart * 60;
    const closeS = target * 3600 + city.hotWindowEnd * 60;

    // If currently inside this window, return 0
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
  return (tomorrowOpenS - currentS) * 1000;
}

export function processObservationFetches(
  fetches: { source: string; promise: Promise<ObservationResult | null> }[],
  onObservation: (obs: ObservationResult | null, source: string) => void,
): void {
  // Fire-and-forget: don't let a slow fetcher block the sleep interval.
  // onObservation is called as soon as each individual fetch resolves.
  for (const { source, promise } of fetches) {
    promise.then(obs => onObservation(obs, source)).catch(() => undefined);
  }
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
    for (const ac of controllers) ac.abort();
  };

  await Promise.all(sources.map(async ({ source, fetch }) => {
    while (!stop && isActive()) {
      const ac = new AbortController();
      controllers.add(ac);
      const timeout = setTimeout(() => ac.abort(), 5_000);

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
  icao: string,
  timezone: string,
  ref: { value: number },
  peakTriggered: { value: boolean },
  bucketMap: Map<string, BucketState>,
  tradeBuckets: BucketState[],
  clob: ClobClient,
  config: Config,
  allowNoTrades = true,
): void {
  if (!obs) return;

  const runNo = allowNoTrades && (config.strategy === "no" || config.strategy === "both");
  const runPeak = config.strategy === "peak" || config.strategy === "both";
  const runDailyPeakTrigger = runPeak && config.dailyPeakTrigger;

  if (obs.tempC > ref.value) {
    const prevValue = ref.value;
    ref.value = obs.tempC;
    const detectedAtMs = Date.now();
    const t0 = performance.now();

    updateCity(icao, prevValue === -Infinity ? undefined : prevValue, obs.tempC, obs.observedAtUtcMs, detectedAtMs);

    if (runNo) {
      evaluateBuckets(obs.tempC, tradeBuckets, b => {
        postOrder(clob, b, config);
      });
    }

    const elapsedUs = Math.round((performance.now() - t0) * 1000);
    const prev = prevValue === -Infinity ? "-∞" : String(prevValue);
    log(source, `observedMax ${prev} → ${obs.tempC} metar=${formatBrt(obs.observedAtUtcMs)} hotPath=${elapsedUs}µs`);

    if (runDailyPeakTrigger) {
      const metarLocalHour = getLocalHour(obs.observedAtUtcMs, timezone);
      evaluatePeakAtHour(ref.value, tradeBuckets, metarLocalHour, config.peakTriggerHour, peakTriggered, (yesBucket, noBucket) => {
        log(source, `peak-at-hour observedMax=${ref.value} metarHour=${metarLocalHour} → YES ${formatBucket(yesBucket)} NO ${noBucket ? formatBucket(noBucket) : "none"}`);
        postPeakOrders(clob, yesBucket, noBucket, config);
      });
    }
  } else if (runPeak && ref.value !== -Infinity) {
    const metarLocalHour = getLocalHour(obs.observedAtUtcMs, timezone);

    if (obs.tempC < ref.value) {
      const localHour = getLocalHour(Date.now(), timezone);
      evaluatePeakDrop(ref.value, tradeBuckets, localHour, peakTriggered, (yesBucket, noBucket) => {
        log(source, `peak drop detected observedMax=${ref.value} current=${obs.tempC} localHour=${localHour} → YES ${formatBucket(yesBucket)} NO ${noBucket ? formatBucket(noBucket) : "none"}`);
        postPeakOrders(clob, yesBucket, noBucket, config);
      });
    }

    if (config.dailyPeakTrigger) {
      evaluatePeakAtHour(ref.value, tradeBuckets, metarLocalHour, config.peakTriggerHour, peakTriggered, (yesBucket, noBucket) => {
        log(source, `peak-at-hour observedMax=${ref.value} metarHour=${metarLocalHour} → YES ${formatBucket(yesBucket)} NO ${noBucket ? formatBucket(noBucket) : "none"}`);
        postPeakOrders(clob, yesBucket, noBucket, config);
      });
    }
  }
}

export async function runHotWindowLoop(
  city: CityConfig,
  config: Config,
  clob: ClobClient,
  buckets: BucketState[],
  observedMaxRef: { value: number },
  deadline: number,
): Promise<void> {
  const peakTriggered = { value: false };
  const bucketMap = new Map(buckets.map(b => [b.noTokenId, b]));
  const exactBuckets = buckets.filter(b => b.type === "exact");
  const exactTokenIds = exactBuckets.map(b => b.noTokenId);
  const wsKey = [...exactTokenIds].sort().join(",");

  if (!config.dryRun) {
    startBookStream(exactTokenIds, city.icao);
    // If we're already inside a hot window, don't sleep — start fetching
    // immediately. The 300ms settle is only needed when we have time before
    // the window opens.
    const inWindowNow = city.targetHours.some(h =>
      isHotWindowMs(Date.now(), h, city.hotWindowStart, city.hotWindowEnd, city.timezone)
    );
    if (!inWindowNow) await Bun.sleep(300);
    // Warm-up HTTP connections in parallel with prepareOrders — both are
    // independent and the fetcher warm-up takes ~700ms cold. Overlapping
    // shaves that time off the critical path before the hot window opens.
    void fetchNoaa(city.icao).catch(() => undefined);
    void fetchAviationWeather(city.icao).catch(() => undefined);
    if (exactTokenIds.length > 0) void clob.getOrderBook(exactTokenIds[0]!).catch(() => undefined);
    // Ensure prepared orders are ready before we enter the hot window.
    // Slow path (on-the-fly createOrder) adds 50–200ms which loses races.
    await prepareOrders(clob, buckets, config);
  }

  while (Date.now() < deadline) {
    const nowMs = Date.now();
    const activeHour = city.targetHours.find(h => isHotWindowMs(nowMs, h, city.hotWindowStart, city.hotWindowEnd, city.timezone));

    if (activeHour !== undefined) {
      if (!config.dryRun) {
        void refreshBooks(clob, exactTokenIds);
        // prepareOrders was already awaited at init; re-checking is cheap but unnecessary.
        if (!isBookStreamConnected(wsKey)) {
          forceReconnectBookStream(wsKey);
        }
      }
      log(city.icao, `hot window open targetHour=${activeHour}`);

      const prevHour = (activeHour - 1 + 24) % 24;

      const refreshInterval = !config.dryRun
        ? setInterval(() => refreshBooks(clob, exactTokenIds).catch(() => undefined), 5_000)
        : undefined;

      while (isHotWindowMs(Date.now(), activeHour, city.hotWindowStart, city.hotWindowEnd, city.timezone)) {
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
          () => isHotWindowMs(Date.now(), activeHour, city.hotWindowStart, city.hotWindowEnd, city.timezone),
          obs => getLocalHour(obs.observedAtUtcMs, city.timezone) !== prevHour,
          (obs, source) => handleObs(obs, source, city.icao, city.timezone, observedMaxRef, peakTriggered, bucketMap, exactBuckets, clob, config, true),
        );
      }

      if (refreshInterval) clearInterval(refreshInterval);

      log(city.icao, `hot window closed targetHour=${activeHour}`);
      // Calculate exact ms remaining in this hot window and sleep once
      const closeS = activeHour * 3600 + city.hotWindowEnd * 60;
      const nowS = getLocalSecondsSinceMidnight(Date.now(), city.timezone);
      const remainingMs = Math.max(0, (closeS - nowS) * 1000 + 100); // +100ms buffer
      if (remainingMs > 0) await Bun.sleep(remainingMs);
    } else {
      processObservationFetches(
        [
          { source: `noaa/${city.icao}`, promise: fetchNoaa(city.icao) },
          { source: `aw/${city.icao}`, promise: fetchAviationWeather(city.icao) },
        ],
        (obs, source) => handleObs(obs, source, city.icao, city.timezone, observedMaxRef, peakTriggered, bucketMap, exactBuckets, clob, config, false),
      );
      if (!config.dryRun) await refreshBooks(clob, exactTokenIds);

      const preciseSleep = msUntilNextHotWindow(Date.now(), city);
      // If a hot window is imminent (< 2 min), wake up precisely then;
      // otherwise use the standard long sleep to avoid busy-waiting.
      const sleepMs = preciseSleep > 0 && preciseSleep < 120_000 ? preciseSleep : 600_000;
      await Bun.sleep(sleepMs);
    }
  }
}
