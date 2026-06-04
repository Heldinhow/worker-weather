#!/bin/bash
set -euo pipefail

# Run benchmark once. It internally runs 100 iterations for stable microsecond timings.
bun run src/benchmark.ts 2>&1 | grep "^METRIC "
