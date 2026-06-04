const DAY_S = 86_400;

export function formatBrt(dateOrMs: Date | number): string {
  const ms = typeof dateOrMs === "number" ? dateOrMs : dateOrMs.getTime();
  const brt = new Date(ms - 3 * 3600_000);
  return brt.toISOString().replace("T", " ").slice(0, 23) + " BRT";
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
