import { appendFile } from "node:fs/promises";
import { formatBrt } from "./time.ts";

export { formatBrt } from "./time.ts";

const ANSI_COLORS = [
  "\x1b[36m", // cyan
  "\x1b[33m", // yellow
  "\x1b[35m", // magenta
  "\x1b[32m", // green
  "\x1b[34m", // blue
];
const RESET = "\x1b[0m";
const useColor = process.stdout.isTTY === true;

const icaoColor = new Map<string, string>();

export function registerCityColor(icao: string, index: number): void {
  icaoColor.set(icao, ANSI_COLORS[index % ANSI_COLORS.length]!);
}

const TAG_WIDTH = 14;
const logFile = process.env.LOG_FILE;

// Pre-compute exact tag → color mapping for O(1) lookup instead of O(n) iteration
let tagColorCache: Map<string, string> | null = null;
function getTagColor(tag: string): string | undefined {
  if (tagColorCache) return tagColorCache.get(tag);
  tagColorCache = new Map<string, string>();
  for (const [icao, color] of icaoColor) {
    // Register colors for known source prefixes
    tagColorCache.set(icao, color);
    tagColorCache.set(`noaa/${icao}`, color);
    tagColorCache.set(`aw/${icao}`, color);
    tagColorCache.set(`book-ws/${icao}`, color);
    tagColorCache.set(`sim/${icao}`, color);
    tagColorCache.set(`bench/${icao}`, color);
  }
  return tagColorCache.get(tag);
}

export function log(tag: string, msg: string): void {
  const bracket = `[${tag}]`;
  let styledTag = bracket.padEnd(TAG_WIDTH);

  if (useColor) {
    const color = getTagColor(tag);
    if (color) {
      styledTag = `${color}${bracket}${RESET}`.padEnd(TAG_WIDTH + color.length + RESET.length);
    }
  }

  const timestamp = formatBrt(Date.now());

  process.stdout.write(`${timestamp} ${styledTag} ${msg}\n`);

  if (logFile) {
    // Fire-and-forget async write — sync appendFileSync blocks the event loop
    // and can stall the hot path by 1–5ms on busy disks.
    appendFile(logFile, `${timestamp} ${bracket.padEnd(TAG_WIDTH)} ${msg}\n`).catch(() => {});
  }
}
