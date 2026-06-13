#!/usr/bin/env bash
set -euo pipefail

# ── Clean ────────────────────────────────────────────────────────────────────
# Removes all build, test, and coverage artifacts.

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(dirname "$SCRIPT_DIR")"

echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "Cleaning build artifacts"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"

rm -rf "${REPO_ROOT}/artifacts"
dotnet clean McpCapabilities.slnx --nologo --verbosity quiet 2>/dev/null || true

echo "✓ Clean complete."
