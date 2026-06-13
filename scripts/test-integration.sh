#!/usr/bin/env bash
set -euo pipefail

# ── Integration Tests ────────────────────────────────────────────────────────
# Runs integration tests (real MCP transport, I/O, external dependencies).
# Usage: ./scripts/test-integration.sh [--configuration <Debug|Release>]

CONFIGURATION="Release"

while [[ $# -gt 0 ]]; do
  case "$1" in
    --configuration|-c) CONFIGURATION="$2"; shift 2 ;;
    *) echo "Unknown option: $1"; exit 1 ;;
  esac
done

echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "Running integration tests (${CONFIGURATION})"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"

dotnet test tests/McpCapabilities.Server.Integration.Tests/McpCapabilities.Server.Integration.Tests.csproj \
  --configuration "${CONFIGURATION}" \
  -- \
  --no-progress \
  --ignore-exit-code "5;8"

echo ""
echo "✓ Integration tests passed."
