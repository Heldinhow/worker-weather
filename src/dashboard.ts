import type { CityConfig } from "./config.ts";
import { isHotWindow, formatBrt } from "./time.ts";

export type CityDashboardState = {
  previousObservedMax: number | undefined;
  observedMax: number;
  metarTimestampMs: number;
  detectedAtMs: number;
};

const cityState = new Map<string, CityDashboardState>();
let registeredCities: CityConfig[] = [];
let started = false;
const useTTY = process.stdout.isTTY === true;

const SEP = "─".repeat(102);
const GREEN = "\x1b[32m";
const RESET = "\x1b[0m";
const SAVE = "\x1b7";
const RESTORE = "\x1b8";

export function updateCity(icao: string, previousObservedMax: number | undefined, observedMax: number, metarTimestampMs: number, detectedAtMs: number): void {
  cityState.set(icao, { previousObservedMax, observedMax, metarTimestampMs, detectedAtMs });
}

function blockHeight(): number {
  return registeredCities.length + 4;
}

function rows(): number {
  return process.stdout.rows ?? 24;
}

function cityName(slug: string): string {
  return slug.split("-").map(part => part[0]!.toUpperCase() + part.slice(1)).join(" ");
}

function tempLabel(city: CityConfig, tempC: number | undefined): string {
  if (tempC === undefined) return "—";
  if (!city.icao.startsWith("K")) return `${tempC}°C`;
  const tempF = Math.round(tempC * 9 / 5 + 32);
  return `${tempC}°C/${tempF}°F`;
}

function renderBlock(): string {
  const now = new Date();
  const lines: string[] = [];

  lines.push(SEP);
  lines.push(` ${"ICAO".padEnd(7)}${"City".padEnd(16)}${"PrevMax".padEnd(14)}${"ObsMax".padEnd(14)}${"METAR".padEnd(12)}${"Detected at".padEnd(15)}HW`);
  lines.push(SEP);

  for (const city of registeredCities) {
    const s = cityState.get(city.icao);
    const activeHour = city.targetHours.find(h =>
      isHotWindow(now, h, city.hotWindowStart, city.hotWindowEnd, city.timezone)
    );

    let hwStr: string;
    if (activeHour !== undefined) {
      const label = `YES (${activeHour}h)`;
      hwStr = useTTY ? `${GREEN}${label}${RESET}` : label;
    } else {
      hwStr = "no";
    }

    if (!s) {
      lines.push(` ${city.icao.padEnd(7)}${cityName(city.slug).padEnd(16)}${"—".padEnd(14)}${"—".padEnd(14)}${"—".padEnd(12)}${"—".padEnd(15)}${hwStr}`);
    } else {
      const prevMax = tempLabel(city, s.previousObservedMax).padEnd(14);
      const obsMax = tempLabel(city, s.observedMax).padEnd(14);
      const metar = (formatBrt(s.metarTimestampMs).slice(11, 16) + " BRT").padEnd(12);
      const detected = (formatBrt(s.detectedAtMs).slice(11, 19) + " BRT").padEnd(15);
      lines.push(` ${city.icao.padEnd(7)}${cityName(city.slug).padEnd(16)}${prevMax}${obsMax}${metar}${detected}${hwStr}`);
    }
  }

  lines.push(SEP);
  // \x1b[K clears to end of line — keeps dashboard clean after a terminal resize
  return lines.map(l => l + "\x1b[K").join("\n") + "\n";
}

function drawDashboard(): void {
  const r = rows();
  const bh = blockHeight();
  const dashStart = r - bh + 1;
  process.stdout.write(SAVE);
  process.stdout.write(`\x1b[${dashStart};1H`);
  process.stdout.write(renderBlock());
  process.stdout.write(RESTORE);
}

function setupLayout(): void {
  const r = rows();
  const bh = blockHeight();
  const logBottom = r - bh;
  // Reserve bottom bh lines for the dashboard; logs scroll in rows 1..logBottom
  process.stdout.write(`\x1b[1;${logBottom}r`);
  // Park cursor at the bottom of the log area so the next log write scrolls correctly
  process.stdout.write(`\x1b[${logBottom};1H`);
  drawDashboard();
}

function teardown(): void {
  // Restore full scrolling region and leave cursor on a clean line
  process.stdout.write("\x1b[r");
  process.stdout.write(`\x1b[${rows()};1H\n`);
}

export function startDashboard(cities: CityConfig[]): void {
  if (!useTTY) return;
  registeredCities = cities;
  started = true;

  process.stdout.write("\x1b[2J\x1b[H"); // clear screen, cursor to home
  setupLayout();

  setInterval(drawDashboard, 1_000);

  process.stdout.on("resize", setupLayout);

  process.on("SIGINT", () => { teardown(); process.exit(0); });
  process.on("SIGTERM", () => { teardown(); process.exit(0); });
}
