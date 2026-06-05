import { describe, expect, test } from "bun:test";
import { evaluateBuckets, evaluatePeakAtHour } from "./evaluator.ts";
import type { BucketState, TemperatureUnit } from "./types.ts";

function bucket(
  label: string,
  lowerTemp: number,
  upperTemp: number,
  unit: TemperatureUnit = "C",
  type: BucketState["type"] = "exact",
): BucketState {
  return {
    tempC: upperTemp,
    lowerTemp,
    upperTemp,
    unit,
    label,
    type,
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
    const buckets = [bucket("70-71°F", 70, 71, "F", "range")];
    const posted: string[] = [];

    evaluateBuckets(21.6, buckets, b => posted.push(b.label));
    expect(posted).toEqual([]);

    evaluateBuckets(22.3, buckets, b => posted.push(b.label));
    expect(posted).toEqual(["70-71°F"]);
  });

  test("does not contest a Fahrenheit range bucket until the whole degree passes its upper bound", () => {
    const buckets = [bucket("78-79°F", 78, 79, "F", "range")];
    const posted: string[] = [];

    evaluateBuckets(26.05, buckets, b => posted.push(b.label));
    expect(posted).toEqual([]);

    evaluateBuckets(26.7, buckets, b => posted.push(b.label));
    expect(posted).toEqual(["78-79°F"]);
  });
});

describe("evaluatePeakAtHour", () => {
  test("selects the Celsius exact bucket containing the observed whole Celsius peak", () => {
    const buckets = [
      bucket("26°C", 26, 26),
      bucket("27°C", 27, 27),
    ];
    const posted: string[] = [];

    evaluatePeakAtHour(26.7, buckets, 15, 15, { value: false }, (yesBucket, noBucket) => {
      posted.push(`${yesBucket.label}/${noBucket?.label ?? "none"}`);
    });

    expect(posted).toEqual(["26°C/27°C"]);
  });

  test("skips range buckets — Peak only operates on exact °C markets", () => {
    const buckets = [
      bucket("78-79°F", 78, 79, "F", "range"),
      bucket("80-81°F", 80, 81, "F", "range"),
    ];
    const posted: string[] = [];

    evaluatePeakAtHour(26.7, buckets, 15, 15, { value: false }, (yesBucket, noBucket) => {
      posted.push(`${yesBucket.label}/${noBucket?.label ?? "none"}`);
    });

    expect(posted).toEqual([]);
  });
});
