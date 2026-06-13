#!/usr/bin/env bash
set -euo pipefail

# ── Build ────────────────────────────────────────────────────────────────────
# Compiles the entire solution with warnings-as-errors.
# Usage: ./scripts/build.sh [--configuration <Debug|Release>]

CONFIGURATION="Release"

while [[ $# -gt 0 ]]; do
  case "$1" in
    --configuration|-c) CONFIGURATION="$2"; shift 2 ;;
    *) echo "Unknown option: $1"; exit 1 ;;
  esac
done

echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "Building solution (${CONFIGURATION})"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"

dotnet build McpCapabilities.slnx \
  --configuration "${CONFIGURATION}" \
  --nologo \
  -warnaserror

echo ""
echo "✓ Build succeeded."
