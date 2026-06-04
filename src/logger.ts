import { appendFileSync } from "fs";
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

  const timestamp = formatBrt(Date.now());

  process.stdout.write(`${timestamp} ${styledTag} ${msg}\n`);

  if (logFile) {
    try {
      appendFileSync(logFile, `${timestamp} ${bracket.padEnd(TAG_WIDTH)} ${msg}\n`);
    } catch {
      // file write errors must not affect the bot
    }
  }
}
