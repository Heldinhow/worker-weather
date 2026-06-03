import cities from "../cities.json";

export type CityConfig = {
  slug: string;
  icao: string;
  timezone: string;
  targetHours: number[];
};

export type Config = {
  cities: CityConfig[];
  maxStake: number;
  minShares: number;
  maxNoPrice: number;
  prod: boolean;
  dryRun: boolean;
  privateKey: string;
  funderAddress: string | undefined;
  signatureType: string | undefined;
};

export function loadConfig(): Config {
  const privateKey = process.env.PRIVATE_KEY;
  if (!privateKey || !privateKey.startsWith("0x")) {
    throw new Error("PRIVATE_KEY is required and must start with 0x");
  }

  return {
    cities: cities as CityConfig[],
    maxStake: Number(process.env.MAX_STAKE ?? "4"),
    minShares: Number(process.env.MIN_SHARES ?? "5"),
    maxNoPrice: Number(process.env.MAX_NO_PRICE ?? "0.95"),
    prod: (process.env.PROD ?? "false") === "true",
    dryRun: (process.env.DRY_RUN ?? "false") === "true",
    privateKey,
    funderAddress: process.env.POLY_FUNDER_ADDRESS || undefined,
    signatureType: process.env.POLY_SIGNATURE_TYPE || undefined,
  };
}
