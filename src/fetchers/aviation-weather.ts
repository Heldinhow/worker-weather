import type { ObservationResult } from "../types.ts";

const UA = "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";

interface AwMetar {
  temp: number;
  reportTime: string;
}

export async function fetchAviationWeather(icao: string, signal?: AbortSignal): Promise<ObservationResult | null> {
  let fetchSignal = signal;
  let timeout: ReturnType<typeof setTimeout> | undefined;
  if (!fetchSignal) {
    const ac = new AbortController();
    timeout = setTimeout(() => ac.abort(), 12_000);
    fetchSignal = ac.signal;
  }

  try {
    const resp = await fetch(
      `https://aviationweather.gov/api/data/metar?ids=${icao}&format=json&metar=true&hours=3`,
      {
        signal: fetchSignal,
        headers: { "Accept": "application/json", "User-Agent": UA },
      },
    );
    if (timeout) clearTimeout(timeout);
    if (!resp.ok) return null;

    const data = await resp.json() as AwMetar[];
    if (!data || data.length === 0) return null;

    const latest = data[0]!;
    const tempC = latest.temp;
    const observedAtUtcMs = new Date(latest.reportTime).getTime();

    if (!isFinite(tempC) || !isFinite(observedAtUtcMs)) return null;

    return { tempC, observedAtUtcMs };
  } catch {
    if (timeout) clearTimeout(timeout);
    return null;
  }
}
