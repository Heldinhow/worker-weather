export type BucketState = {
  tempC: number;
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
