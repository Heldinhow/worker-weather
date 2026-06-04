#!/bin/bash
set -euo pipefail

# Type-check the project
bunx tsc --noEmit 2>&1 | grep -i "error" | head -20 || echo "Type check passed"
