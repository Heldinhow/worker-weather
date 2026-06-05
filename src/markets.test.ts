import { describe, expect, test } from "bun:test";
import { parseGammaMarket } from "./markets.ts";
import type { GammaEvent, GammaMarket } from "./markets.ts";

const event: GammaEvent = { negRisk: true, markets: [] };

function market(groupItemTitle: string, question = groupItemTitle): GammaMarket {
  return {
    groupItemTitle,
    question,
    clobTokenIds: JSON.stringify([`yes-${groupItemTitle}`, `no-${groupItemTitle}`]),
    conditionId: `condition-${groupItemTitle}`,
  };
}

describe("parseGammaMarket", () => {
  test("parses Celsius exact buckets without defaulting to zero", () => {
    const bucket = parseGammaMarket(event, market("17°C"));

    expect(bucket).toMatchObject({
      lowerTemp: 17,
      upperTemp: 17,
      tempC: 17,
      unit: "C",
      type: "exact",
      label: "17°C",
    });
  });

  test("parses Fahrenheit range buckets as finite tradable buckets", () => {
    const bucket = parseGammaMarket(event, market("70-71°F", "Will the highest temperature be between 70-71°F?"));

    expect(bucket).toMatchObject({
      lowerTemp: 70,
      upperTemp: 71,
      tempC: 71,
      unit: "F",
      type: "range",
      label: "70-71°F",
    });
  });

  test("parses Fahrenheit edge buckets but keeps their type", () => {
    expect(parseGammaMarket(event, market("69°F or below"))).toMatchObject({
      lowerTemp: 69,
      upperTemp: 69,
      unit: "F",
      type: "below",
    });
    expect(parseGammaMarket(event, market("88°F or higher"))).toMatchObject({
      lowerTemp: 88,
      upperTemp: Infinity,
      tempC: 88,
      unit: "F",
      type: "above",
    });
  });

  test("returns null for unknown temperature shapes", () => {
    expect(parseGammaMarket(event, market("not a temperature"))).toBeNull();
  });
});
