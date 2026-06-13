#!/usr/bin/env bash
set -euo pipefail

# ── All Tests ────────────────────────────────────────────────────────────────
# Runs both unit and integration tests.
# Usage: ./scripts/test.sh [--configuration <Debug|Release>]

CONFIGURATION="Release"
FAILED=0

while [[ $# -gt 0 ]]; do
  case "$1" in
    --configuration|-c) CONFIGURATION="$2"; shift 2 ;;
    *) echo "Unknown option: $1"; exit 1 ;;
  esac
done

echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "Running all tests (${CONFIGURATION})"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"

# Unit tests
echo ""
echo "▸ Unit tests…"
if dotnet test tests/McpCapabilities.Server.Unit.Tests/McpCapabilities.Unit.Tests.csproj \
  --configuration "${CONFIGURATION}" \
  -- \
  --no-progress \
  --ignore-exit-code "5;8"; then
  echo "  ✓ Unit tests passed."
else
  echo "  ✗ Unit tests FAILED."
  FAILED=1
fi

# Integration tests
echo ""
echo "▸ Integration tests…"
if dotnet test tests/McpCapabilities.Server.Integration.Tests/McpCapabilities.Server.Integration.Tests.csproj \
  --configuration "${CONFIGURATION}" \
  -- \
  --no-progress \
  --ignore-exit-code "5;8"; then
  echo "  ✓ Integration tests passed."
else
  echo "  ✗ Integration tests FAILED."
  FAILED=1
fi

echo ""
if [[ $FAILED -eq 0 ]]; then
  echo "✓ All tests passed."
else
  echo "✗ One or more test suites failed."
  exit 1
fi
