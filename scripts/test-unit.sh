#!/usr/bin/env bash
set -euo pipefail

# ── Unit Tests ───────────────────────────────────────────────────────────────
# Runs only unit tests (fast, no I/O, no network).
# Usage: ./scripts/test-unit.sh [--configuration <Debug|Release>]

CONFIGURATION="Release"

while [[ $# -gt 0 ]]; do
  case "$1" in
    --configuration|-c) CONFIGURATION="$2"; shift 2 ;;
    *) echo "Unknown option: $1"; exit 1 ;;
  esac
done

echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "Running unit tests (${CONFIGURATION})"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"

dotnet test tests/McpCapabilities.Server.Unit.Tests/McpCapabilities.Unit.Tests.csproj \
  --configuration "${CONFIGURATION}" \
  -- \
  --no-progress \
  --ignore-exit-code "5;8"

echo ""
echo "✓ Unit tests passed."
