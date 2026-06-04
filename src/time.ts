const DAY_S = 86_400;

const MONTHS = [
  "january", "february", "march", "april", "may", "june",
  "july", "august", "september", "october", "november", "december",
];

export function todaySlug(citySlug: string): string {
  const brt = new Date(Date.now() - 3 * 3600_000);
  const month = MONTHS[brt.getUTCMonth()]!;
  const day = brt.getUTCDate();
  const year = brt.getUTCFullYear();
  return `highest-temperature-in-${citySlug}-on-${month}-${day}-${year}`;
}

let lastFormatMs = -1;
let lastFormatResult = "";

export function formatBrt(dateOrMs: Date | number): string {
  const ms = typeof dateOrMs === "number" ? dateOrMs : dateOrMs.getTime();
  if (ms === lastFormatMs) return lastFormatResult;
  lastFormatMs = ms;
  const brt = new Date(ms - 3 * 3600_000);
  lastFormatResult = brt.toISOString().replace("T", " ").slice(0, 23) + " BRT";
  return lastFormatResult;
}

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

export function isHotWindowMs(
  nowMs: number,
  targetHourBrt: number,
  windowStart: number,
  windowEnd: number,
): boolean {
  const { h, m } = brtHourMin(nowMs);
  const prevH = (targetHourBrt - 1 + 24) % 24;
  return (h === prevH && m >= windowStart) || (h === targetHourBrt && m <= windowEnd);
}
