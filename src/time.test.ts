import { describe, expect, test } from "bun:test";
import cities from "../cities.json";
import { isHotWindowMs } from "./time.ts";

describe("isHotWindowMs", () => {
  test("uses the city's timezone instead of BRT", () => {
    const laTenFiftyThree = Date.UTC(2026, 5, 5, 17, 53);
    const brtElevenFiftyThree = Date.UTC(2026, 5, 5, 14, 53);

    expect(isHotWindowMs(laTenFiftyThree, 11, 52, 57, "America/Los_Angeles")).toBe(true);
    expect(isHotWindowMs(brtElevenFiftyThree, 11, 52, 57, "America/Los_Angeles")).toBe(false);
  });

  test("still handles Sao Paulo local windows", () => {
    const saoPauloTenFiftySix = Date.UTC(2026, 5, 5, 13, 56);
    const saoPauloEleven = Date.UTC(2026, 5, 5, 14, 0);

    expect(isHotWindowMs(saoPauloTenFiftySix, 11, 55, 0, "America/Sao_Paulo")).toBe(true);
    expect(isHotWindowMs(saoPauloEleven, 11, 55, 0, "America/Sao_Paulo")).toBe(true);
  });

  test("Buenos Aires window does not open at the top of the previous hour", () => {
    const buenosAires = cities.find(city => city.slug === "buenos-aires")!;
    const buenosAiresTenOhNine = Date.UTC(2026, 5, 5, 13, 9, 56);
    const buenosAiresTenFiftySix = Date.UTC(2026, 5, 5, 13, 56);

    expect(isHotWindowMs(buenosAiresTenOhNine, 11, buenosAires.hotWindowStart, buenosAires.hotWindowEnd, buenosAires.timezone)).toBe(false);
    expect(isHotWindowMs(buenosAiresTenFiftySix, 11, buenosAires.hotWindowStart, buenosAires.hotWindowEnd, buenosAires.timezone)).toBe(true);
  });
});
