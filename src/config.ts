export type CityConfig = {
  slug: string;
  icao: string;
  timezone: string;
};

export type Config = {
  cities: CityConfig[];
  targetHours: number[];
  maxStake: number;
  minShares: number;
  prod: boolean;
  privateKey: string;
  funderAddress: string | undefined;
  signatureType: string | undefined;
};

export function loadConfig(): Config {
  const privateKey = process.env.PRIVATE_KEY;
  if (!privateKey || !privateKey.startsWith("0x")) {
    throw new Error("PRIVATE_KEY is required and must start with 0x");
  }

  const citiesStr = process.env.CITIES;
  if (!citiesStr) {
    throw new Error("CITIES is required");
  }

  return {
    cities: JSON.parse(citiesStr) as CityConfig[],
    targetHours: (process.env.TARGET_HOURS ?? "10,11,12,13,14,15,16")
      .split(",")
      .map(Number),
    maxStake: Number(process.env.MAX_STAKE ?? "4"),
    minShares: Number(process.env.MIN_SHARES ?? "5"),
    prod: (process.env.PROD ?? "false") === "true",
    privateKey,
    funderAddress: process.env.POLY_FUNDER_ADDRESS || undefined,
    signatureType: process.env.POLY_SIGNATURE_TYPE || undefined,
  };
}
