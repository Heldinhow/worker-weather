import { describe, expect, test } from "bun:test";
import { evaluateBuckets, evaluatePeakAtHour } from "./evaluator.ts";
import type { BucketState, TemperatureUnit } from "./types.ts";

function bucket(label: string, lowerTemp: number, upperTemp: number, unit: TemperatureUnit = "C"): BucketState {
  return {
    tempC: upperTemp,
    lowerTemp,
    upperTemp,
    unit,
    label,
    type: "exact",
    noTokenId: `no-${label}`,
    yesTokenId: `yes-${label}`,
    conditionId: `condition-${label}`,
    negRisk: true,
    bought: false,
    attempted: false,
    pendingBuy: false,
    peakBought: false,
  };
}

describe("evaluateBuckets", () => {
  test("does not contest a Celsius bucket on a fractional same-degree observation", () => {
    const buckets = [bucket("12°C", 12, 12)];
    const posted: string[] = [];

    evaluateBuckets(12.2, buckets, b => posted.push(b.label));

    expect(posted).toEqual([]);
    expect(buckets[0]!.pendingBuy).toBe(false);
  });

  test("contests a Celsius bucket when the observed whole degree is higher", () => {
    const buckets = [bucket("12°C", 12, 12)];
    const posted: string[] = [];

    evaluateBuckets(13, buckets, b => posted.push(b.label));

    expect(posted).toEqual(["12°C"]);
  });

  test("uses Fahrenheit whole degrees for Fahrenheit range buckets", () => {
    const buckets = [bucket("70-71°F", 70, 71, "F")];
    const posted: string[] = [];

    evaluateBuckets(21.6, buckets, b => posted.push(b.label));
    expect(posted).toEqual([]);

    evaluateBuckets(22.3, buckets, b => posted.push(b.label));
    expect(posted).toEqual(["70-71°F"]);
  });
});

describe("evaluatePeakAtHour", () => {
  test("selects the Fahrenheit range containing the observed whole Fahrenheit peak", () => {
    const buckets = [
      bucket("70-71°F", 70, 71, "F"),
      bucket("72-73°F", 72, 73, "F"),
    ];
    const posted: string[] = [];

    evaluatePeakAtHour(22.3, buckets, 15, 15, { value: false }, (yesBucket, noBucket) => {
      posted.push(`${yesBucket.label}/${noBucket?.label ?? "none"}`);
    });

    expect(posted).toEqual(["72-73°F/none"]);
  });
});
