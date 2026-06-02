import type { ClobClient } from "@polymarket/clob-client-v2";
import type { BucketState, ObservationResult } from "./types.ts";
import type { CityConfig, Config } from "./config.ts";
import { fetchNoaa } from "./fetchers/noaa.ts";
import { fetchAviationWeather } from "./fetchers/aviation-weather.ts";
import { evaluateBuckets } from "./evaluator.ts";
import { postOrder } from "./trader.ts";
import { log } from "./logger.ts";

export function isHotWindow(now: Date, targetHourBrt: number): boolean {
  const brt = new Date(now.getTime() - 3 * 3600_000);
  const h = brt.getUTCHours();
  const m = brt.getUTCMinutes();
  const prevH = (targetHourBrt - 1 + 24) % 24;
  return (h === prevH && m >= 53) || (h === targetHourBrt && m <= 4);
}

function getBrtHour(now: Date): number {
  return new Date(now.getTime() - 3 * 3600_000).getUTCHours();
}

function handleObs(
  obs: ObservationResult | null,
  source: string,
  ref: { value: number },
  buckets: BucketState[],
  clob: ClobClient,
  config: Config,
): void {
  if (!obs) {
    log(source, "no data");
    return;
  }
  log(source, `tempC=${obs.tempC} metar=${new Date(obs.observedAtUtcMs).toISOString()}`);
  if (obs.tempC <= ref.value) return;
  const prev = ref.value === -Infinity ? "-∞" : String(ref.value);
  log(source, `observedMax ${prev} → ${obs.tempC}`);
  ref.value = obs.tempC;
  evaluateBuckets(ref.value, buckets, tokenId => {
    const b = buckets.find(b => b.noTokenId === tokenId);
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
  while (Date.now() < deadline) {
    const now = new Date();
    const activeHour = config.targetHours.find(h => isHotWindow(now, h));

    if (activeHour !== undefined) {
      log(city.icao, `hot window open targetHour=${activeHour}`);
      while (isHotWindow(new Date(), activeHour)) {
        const ac1 = new AbortController();
        const t1 = setTimeout(() => ac1.abort(), 12_000);
        const ac2 = new AbortController();
        const t2 = setTimeout(() => ac2.abort(), 12_000);

        const p1 = fetchNoaa(city.icao, ac1.signal).then(obs => {
          clearTimeout(t1);
          handleObs(obs, `noaa/${city.icao}`, observedMaxRef, buckets, clob, config);
        });
        const p2 = fetchAviationWeather(city.icao, ac2.signal).then(obs => {
          clearTimeout(t2);
          handleObs(obs, `aw/${city.icao}`, observedMaxRef, buckets, clob, config);
        });
        await Promise.all([p1, p2]);
      }
      log(city.icao, `hot window closed targetHour=${activeHour}`);
    } else {
      const obs1 = await fetchNoaa(city.icao);
      handleObs(obs1, `noaa/${city.icao}`, observedMaxRef, buckets, clob, config);
      const obs2 = await fetchAviationWeather(city.icao);
      handleObs(obs2, `aw/${city.icao}`, observedMaxRef, buckets, clob, config);

      const brtH = getBrtHour(new Date());
      const minH = Math.min(...config.targetHours);
      const maxH = Math.max(...config.targetHours);
      const sleepMs = brtH >= minH && brtH <= maxH ? 30_000 : 600_000;
      await Bun.sleep(sleepMs);
    }
  }
}
