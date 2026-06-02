export type BucketState = {
  tempC: number;
  type: "exact" | "below" | "above";
  noTokenId: string;
  conditionId: string;
  negRisk: boolean;
  bought: boolean;
  attempted: boolean;
  pendingBuy: boolean;
};

export type ObservationResult = {
  tempC: number;
  observedAtUtcMs: number;
};
