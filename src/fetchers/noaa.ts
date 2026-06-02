import type { ObservationResult } from "../types.ts";

const UA = "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";

function parseMetarTemp(metar: string): number | null {
  const m = metar.match(/\b(M?\d{2})\/(M?\d{2})\b/);
  if (!m) return null;
  const s = m[1]!;
  return s.startsWith("M") ? -parseFloat(s.slice(1)) : parseFloat(s);
}

export async function fetchNoaa(icao: string, signal?: AbortSignal): Promise<ObservationResult | null> {
  let fetchSignal = signal;
  let timeout: ReturnType<typeof setTimeout> | undefined;
  if (!fetchSignal) {
    const ac = new AbortController();
    timeout = setTimeout(() => ac.abort(), 12_000);
    fetchSignal = ac.signal;
  }

  try {
    const resp = await fetch(
      `https://tgftp.nws.noaa.gov/data/observations/metar/stations/${icao}.TXT`,
      {
        signal: fetchSignal,
        headers: { "Cache-Control": "no-cache", "User-Agent": UA },
      },
    );
    if (timeout) clearTimeout(timeout);
    if (!resp.ok) return null;

    const text = await resp.text();
    const lines = text.trim().split("\n");
    if (lines.length < 2) return null;

    const line0 = lines[0]!;
    const line1 = lines[1]!;

    const dm = line0.match(/(\d{4})\/(\d{2})\/(\d{2})\s+(\d{2}):(\d{2})/);
    if (!dm) return null;
    const observedAtUtcMs = Date.UTC(+dm[1]!, +dm[2]! - 1, +dm[3]!, +dm[4]!, +dm[5]!);

    const tempC = parseMetarTemp(line1);
    if (tempC === null) return null;

    return { tempC, observedAtUtcMs };
  } catch {
    if (timeout) clearTimeout(timeout);
    return null;
  }
}
