// BRT = UTC-3, fixed (Brazil abolished DST in 2019)
export function formatBrt(dateOrMs: Date | number): string {
  const ms = typeof dateOrMs === "number" ? dateOrMs : dateOrMs.getTime();
  const brt = new Date(ms - 3 * 3600_000);
  return brt.toISOString().replace("T", " ").slice(0, 23) + " BRT";
}

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

const TAG_WIDTH = 14; // wide enough for "noaa/EGLC" + padding

export function log(tag: string, msg: string): void {
  const bracket = `[${tag}]`;
  let styledTag = bracket.padEnd(TAG_WIDTH);

  if (useColor) {
    for (const [icao, color] of icaoColor) {
      if (tag.includes(icao)) {
        styledTag = `${color}${bracket}${RESET}`.padEnd(TAG_WIDTH + color.length + RESET.length);
        break;
      }
    }
  }

  process.stdout.write(`${formatBrt(Date.now())} ${styledTag} ${msg}\n`);
}
