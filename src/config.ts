import cities from "../cities.json";

export type CityConfig = {
  slug: string;
  icao: string;
  timezone: string;
  targetHours: number[];
  hotWindowStart: number;
  hotWindowEnd: number;
  enabled?: boolean;
};

export type Config = {
  cities: CityConfig[];
  maxStake: number;
  minShares: number;
  prod: boolean;
  dryRun: boolean;
  privateKey: string;
  funderAddress: string | undefined;
  signatureType: string | undefined;
  strategy: "no" | "peak" | "both";
  dailyPeakTrigger: boolean;
  peakTriggerHour: number;
  tradeLedgerPath: string;
  metarMaxAgeMs: number;
};

export function loadConfig(): Config {
  const privateKey = process.env.PRIVATE_KEY;
  if (!privateKey || !privateKey.startsWith("0x")) {
    throw new Error("PRIVATE_KEY is required and must start with 0x");
  }

  return {
    cities: (cities as CityConfig[]).filter(city => city.enabled !== false),
    maxStake: Number(process.env.MAX_STAKE ?? "4"),
    minShares: Number(process.env.MIN_SHARES ?? "5"),
    prod: (process.env.PROD ?? "false") === "true",
    dryRun: (process.env.DRY_RUN ?? "false") === "true",
    privateKey,
    funderAddress: process.env.POLY_FUNDER_ADDRESS || undefined,
    signatureType: process.env.POLY_SIGNATURE_TYPE || undefined,
    strategy: (process.env.STRATEGY ?? "no") as "no" | "peak" | "both",
    dailyPeakTrigger: (process.env.DAILY_PEAK_TRIGGER ?? "true") === "true",
    peakTriggerHour: Number(process.env.PEAK_TRIGGER_HOUR ?? "17"),
    tradeLedgerPath: process.env.TRADE_LEDGER_PATH ?? ".state/trades.jsonl",
    metarMaxAgeMs: Number(process.env.METAR_MAX_AGE_MS ?? "900000"),
  };
}
