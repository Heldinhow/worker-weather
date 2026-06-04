# Deferred Optimizations

- **AviationWeather cache-busting**: Add `&_=${Date.now()}` query param similar to NOAA, in case aviationweather.gov has CDN caching.
- **Bun fetch keepalive**: Explicitly pass `keepalive: true` to fetch options to ensure HTTP connection reuse (Bun default may vary by endpoint).
- **WebSocket message parse**: `applyMarketMessage` does `JSON.parse(String(event.data))` and several type casts. Could pre-allocate the normalized orders array or use a faster path for empty messages.
- **Logger tag color cache**: Pre-build a `Map<string, string>` from exact tag → color string instead of iterating icaoColor on every log call.
- **CLOB createOrder warm-up**: `prepareOrders` already warms up, but a dummy `createOrder` with size=0 before `prepareOrders` could warm the server endpoint even earlier.
- **Multi-city parallel prewarm**: Currently each city resolves markets and prepares orders sequentially in `Promise.all`. Could overlap more work.
- **evaluateBuckets binary search**: If exactBuckets grows large (>50), binary search for the threshold tempC would be faster than linear scan.
