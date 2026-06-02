# Contested-NO Weather Bot

A standalone bot that monitors Polymarket temperature bucket markets and buys NO the instant an observed METAR temperature surpasses a bucket threshold. Competes against other automated players; every millisecond of latency on the hot path matters.

## Language

**Hot Window**:
The 10-minute bracket around each Target Hour — from minute 53 of the previous hour through minute 04 of the target hour — during which METAR fetches race concurrently and buckets are evaluated immediately on every new observation.
_Avoid_: fast lane, transition window, polling window

**Target Hour**:
An hour (10h–16h BRT) at which Polymarket temperature markets resolve. Each target hour has a corresponding Hot Window.
_Avoid_: resolution hour, slot

**Bucket**:
A single binary temperature market for one exact °C value (e.g., "exactly 24°C"). Identified by its NO token ID. Only "exact" buckets are traded — "or below" and "or higher" markets are ignored.
_Avoid_: market, temperature market, slot

**ObservedMax**:
The monotonically-increasing maximum temperature seen today across all METAR observations for a given city. Never reset between Hot Windows within the same trading day. Only resets at midnight BRT.
_Avoid_: current temperature, observed temperature, max temp

**Contested NO**:
A NO position on a Bucket where `floor(ObservedMax) > bucket.tempC` — meaning that exact temperature is meteorologically impossible for the day. The bot buys Contested NO immediately on detection.
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

**FOK** (Fill-or-Kill):
An order type that must be filled immediately in full or is cancelled. The only order type the bot posts. If no ask is visible, a blind FOK at 0.99 is sent once and the bucket is marked `attempted` permanently.
_Avoid_: immediate-or-cancel, market order

**BRT** (Brasília Time):
UTC−3, used throughout for date resolution, hot-window detection, and daily reset. Brazil abolished DST in 2019 so this offset is fixed year-round for all supported cities.
_Avoid_: local time, Brazil time, São Paulo time
