export type TemperatureUnit = "C" | "F";

export type BucketState = {
  // Historical name kept for hot-path compatibility. This is the bucket's
  // upper threshold in its market unit, not always Celsius.
  tempC: number;
  lowerTemp: number;
  upperTemp: number;
  unit: TemperatureUnit;
  label: string;
  icao?: string;
  citySlug?: string;
  eventSlug?: string;
  type: "exact" | "below" | "above";
  noTokenId: string;
  yesTokenId: string;
  conditionId: string;
  negRisk: boolean;
  bought: boolean;
  attempted: boolean;
  pendingBuy: boolean;
  peakBought: boolean;
};

export type ObservationResult = {
  tempC: number;
  observedAtUtcMs: number;
};
