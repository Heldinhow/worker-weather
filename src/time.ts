type LocalTimeParts = { h: number; m: number; s: number };

const localTimeFormatters = new Map<string, Intl.DateTimeFormat>();

function localTimeFormatter(timezone: string): Intl.DateTimeFormat {
  let formatter = localTimeFormatters.get(timezone);
  if (!formatter) {
    formatter = new Intl.DateTimeFormat("en-US", {
      hour: "numeric",
      minute: "numeric",
      second: "numeric",
      hourCycle: "h23",
      timeZone: timezone,
    });
    localTimeFormatters.set(timezone, formatter);
  }
  return formatter;
}

function getLocalTimeParts(utcMs: number, timezone: string): LocalTimeParts {
  const parts = localTimeFormatter(timezone).formatToParts(new Date(utcMs));
  return {
    h: Number(parts.find(p => p.type === "hour")!.value),
    m: Number(parts.find(p => p.type === "minute")!.value),
    s: Number(parts.find(p => p.type === "second")!.value),
  };
}

export function getLocalHour(utcMs: number, timezone: string): number {
  return getLocalTimeParts(utcMs, timezone).h;
}

export function getLocalSecondsSinceMidnight(utcMs: number, timezone: string): number {
  const { h, m, s } = getLocalTimeParts(utcMs, timezone);
  return h * 3600 + m * 60 + s + (utcMs % 1000) / 1000;
}

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

export function isHotWindow(
  now: Date,
  targetHourLocal: number,
  windowStart: number,
  windowEnd: number,
  timezone = "America/Sao_Paulo",
): boolean {
  return isHotWindowMs(now.getTime(), targetHourLocal, windowStart, windowEnd, timezone);
}

export function isHotWindowMs(
  nowMs: number,
  targetHourLocal: number,
  windowStart: number,
  windowEnd: number,
  timezone = "America/Sao_Paulo",
): boolean {
  const { h, m } = getLocalTimeParts(nowMs, timezone);
  const prevH = (targetHourLocal - 1 + 24) % 24;
  return (h === prevH && m >= windowStart) || (h === targetHourLocal && m <= windowEnd);
}
