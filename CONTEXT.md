# Contested-NO Weather Bot

A standalone bot that monitors Polymarket temperature bucket markets and buys NO the instant an observed METAR temperature surpasses a bucket threshold. Competes against other automated players; every millisecond of latency on the hot path matters.

## Language

**Hot Window**:
The city-local bracket around each Target Hour — from the configured minute of the previous hour through the configured minute of the target hour — during which METAR fetches race concurrently and buckets are evaluated immediately when each source returns. The faster source must never wait for the slower source.
_Avoid_: fast lane, transition window, polling window

**Target Hour**:
An hour in the city's configured timezone at which Polymarket temperature markets resolve. Each target hour has a corresponding Hot Window.
_Avoid_: resolution hour, slot

**Bucket**:
A single finite binary temperature outcome: either one exact whole-degree °C value (e.g., "17°C") or a finite whole-degree °F range (e.g., "70-71°F"), depending on the Polymarket event unit. Identified by its NO token ID. Open-ended "or below" and "or higher" markets are ignored.
_Avoid_: market, temperature market, slot

**ObservedMax**:
The monotonically-increasing maximum temperature seen today across all METAR observations for a given city, stored in °C. Never reset between Hot Windows within the same trading day. Only resets at midnight BRT. When evaluating a Bucket, ObservedMax is converted to that Bucket's unit and floored to the whole degree used by the market.
_Avoid_: current temperature, observed temperature, max temp

**Contested NO**:
A NO position on a Bucket where `floor(convert(ObservedMax, bucket.unit)) > bucket.upperTemp` — meaning that bucket outcome is meteorologically impossible for the day. For exact °C Buckets, `upperTemp` is the exact value; for °F range Buckets, it is the range's upper bound. The bot buys Contested NO immediately on detection inside a Hot Window. Outside Hot Windows, warm poll observations only seed ObservedMax and the Dashboard.
_Avoid_: arbitrage, contested position

**METAR**:
A standardized aviation weather report issued on the hour by a station identified by its ICAO code. The sole data source for ObservedMax. Fetched from NOAA TGFTP (plain text) and AviationWeather (JSON).
_Avoid_: observation, weather data, station data

**ICAO**:
The 4-letter station code identifying a METAR observation station (e.g., `SBGR` for São Paulo Guarulhos). Each city maps to exactly one ICAO code.
_Avoid_: station code, airport code

**Slug**:
The Polymarket event identifier. Format: `highest-temperature-in-{city}-on-{fullMonth}-{day}-{year}` (full English month name, no zero-padding on day). Example: `highest-temperature-in-sao-paulo-on-june-2-2026`. The `CityConfig.slug` field stores only the city segment (`sao-paulo`); the full slug is composed at runtime.
_Avoid_: market ID, event ID, event key

**FAK** (Fill-and-Kill):
The order type the bot uses for all executions. Fills whatever shares are available in the book immediately and cancels the remainder — never rests in the book. Chosen over FOK because a partial fill on a Contested NO is strictly better than no fill.
_Avoid_: FOK, immediate-or-cancel, market order

**Blind Experiment**:
When the book cache is null (cold start or stale), the bot fires two FAK orders in parallel for the same bucket: a limit FAK at 0.99 and a `createAndPostMarketOrder` FAK. Both results are logged so fill price and execution quality can be compared empirically to inform future strategy.
_Avoid_: blind FOK, fallback order

**Prewarm**:
Before a Hot Window, the bot warms CLOB market metadata and pre-signs one limit FAK plus one market FAK for each exact Bucket. On trigger, the hot path should post prepared orders instead of signing or fetching metadata.
_Avoid_: lazy signing, cold order creation

**Book Stream**:
The websocket market data subscription that keeps Bucket asks fresh with `book` snapshots and `price_change` updates. REST `getOrderBook` remains a fallback refresh, not the primary hot-path source of truth.
_Avoid_: polling-only cache, stale book cache

**BRT** (Brasília Time):
UTC−3, used for logs, date resolution, and daily reset. Hot Window detection uses each city's configured timezone. Brazil abolished DST in 2019 so this offset is fixed year-round for Brazilian cities.
_Avoid_: local time, Brazil time, São Paulo time

**Dashboard**:
A live terminal display refreshed every second that shows per-city bot state: previous ObservedMax, current ObservedMax, the METAR timestamp of the observation that set it, the wall-clock time the bot first detected it, and whether the city is currently in a Hot Window. Replaces routine METAR fetch logs — only detection events (new ObservedMax), trades, and errors produce scroll output and are written to the log file.
_Avoid_: status panel, monitor, TUI

**Daily Peak Trigger**:
A time-triggered strategy that fires once at a configurable city-local hour (default 16h) when `peakTriggered` is still false. On the first METAR whose `observedAt` hour equals the trigger hour, buys YES on the ObservedMax bucket and NO on the bucket immediately above — treating the day's ObservedMax as the confirmed daily peak regardless of whether a temperature drop was detected. Complements Peak Detection: whichever fires first wins via the shared `peakTriggered` flag. Active by default when `strategy` is `"peak"` or `"both"`; can be disabled with `DAILY_PEAK_TRIGGER=false`.
_Avoid_: hourly peak check, peak safety net

**DetectedAt**:
The wall-clock timestamp recorded by the bot the moment it first processes an observation that raises the ObservedMax for a city. Distinct from the METAR timestamp (`observedAtUtcMs`), which is when the station measured the temperature. DetectedAt is relevant for latency analysis; the METAR timestamp is relevant for market resolution.
_Avoid_: processed at, received at
