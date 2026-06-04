import type { ObservationResult } from "../types.ts";

const UA = "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";

function parseMetarTemp(metar: string): number | null {
  // Manual scan for temperature/dewpoint pattern (e.g. 25/18 or M02/M05)
  // Look for slash surrounded by digits, avoiding regex overhead
  for (let i = 1; i < metar.length - 1; i++) {
    if (metar.charCodeAt(i) !== 47) continue; // '/'
    const before = metar.charCodeAt(i - 1);
    const after = metar.charCodeAt(i + 1);
    // Check if pattern is digits/digits or M+digits/M+digits
    const isDigitBefore = before >= 48 && before <= 57;
    const isDigitAfter = after >= 48 && after <= 57;
    const isMbefore = before === 77 && i >= 2 && metar.charCodeAt(i - 2) >= 48 && metar.charCodeAt(i - 2) <= 57;
    const isMafter = after === 77 && i + 2 < metar.length && metar.charCodeAt(i + 2) >= 48 && metar.charCodeAt(i + 2) <= 57;
    if ((isDigitBefore || isMbefore) && (isDigitAfter || isMafter)) {
      // Extract temperature (before slash)
      let start = i - 1;
      if (metar.charCodeAt(start) === 77) start--;
      while (start > 0 && metar.charCodeAt(start - 1) >= 48 && metar.charCodeAt(start - 1) <= 57) start--;
      const s = metar.slice(start, i);
      return s.startsWith("M") ? -parseFloat(s.slice(1)) : parseFloat(s);
    }
  }
  return null;
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
    // Fast path: find first two lines without allocating a split array.
    let i = 0;
    while (i < text.length && (text.charCodeAt(i) === 13 || text.charCodeAt(i) === 10)) i++; // skip leading newlines
    const start0 = i;
    while (i < text.length && text.charCodeAt(i) !== 10 && text.charCodeAt(i) !== 13) i++;
    const line0 = text.slice(start0, i);
    i++;
    while (i < text.length && (text.charCodeAt(i) === 13 || text.charCodeAt(i) === 10)) i++;
    const start1 = i;
    while (i < text.length && text.charCodeAt(i) !== 10 && text.charCodeAt(i) !== 13) i++;
    const line1 = text.slice(start1, i);
    if (!line0 || !line1) return null;

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
