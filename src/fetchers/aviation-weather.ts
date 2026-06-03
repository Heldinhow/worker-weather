import type { ObservationResult } from "../types.ts";

const UA = "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";

// Parse ISO 8601 UTC string manually to avoid new Date() overhead.
// Expects format: YYYY-MM-DDTHH:MM:SSZ (AviationWeather API format).
function parseIsoUtcMs(s: string): number {
  if (s.length < 19) return NaN;
  const year = (s.charCodeAt(0) - 48) * 1000 + (s.charCodeAt(1) - 48) * 100 + (s.charCodeAt(2) - 48) * 10 + (s.charCodeAt(3) - 48);
  const month = (s.charCodeAt(5) - 48) * 10 + (s.charCodeAt(6) - 48) - 1;
  const day = (s.charCodeAt(8) - 48) * 10 + (s.charCodeAt(9) - 48);
  const hour = (s.charCodeAt(11) - 48) * 10 + (s.charCodeAt(12) - 48);
  const minute = (s.charCodeAt(14) - 48) * 10 + (s.charCodeAt(15) - 48);
  const second = (s.charCodeAt(17) - 48) * 10 + (s.charCodeAt(18) - 48);
  return Date.UTC(year, month, day, hour, minute, second);
}

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
    const observedAtUtcMs = parseIsoUtcMs(latest.reportTime);

    if (!isFinite(tempC) || !isFinite(observedAtUtcMs)) return null;

    return { tempC, observedAtUtcMs };
  } catch {
    if (timeout) clearTimeout(timeout);
    return null;
  }
}
