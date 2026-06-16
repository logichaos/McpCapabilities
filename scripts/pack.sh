#!/usr/bin/env bash
set -euo pipefail

# ── Pack ─────────────────────────────────────────────────────────────────────
# Builds the NuGet package for McpCapabilities.Server.
# Version is derived automatically from git tags via MinVer.
#
# Usage: ./scripts/pack.sh [--configuration <Debug|Release>]
#
# To set a release version, create a git tag:
#   git tag v1.2.3
#   ./scripts/pack.sh

CONFIGURATION="Release"

while [[ $# -gt 0 ]]; do
  case "$1" in
    --configuration|-c) CONFIGURATION="$2"; shift 2 ;;
    *) echo "Unknown option: $1"; exit 1 ;;
  esac
done

PROJECT="src/McpCapabilities.Server/McpCapabilities.Server.csproj"
OUTPUT_DIR="artifacts/package/release"

echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "Building NuGet package (${CONFIGURATION})"
echo "Version: from git tags (MinVer)"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"

dotnet pack "${PROJECT}" \
  --configuration "${CONFIGURATION}" \
  --nologo

echo ""
echo "✓ Package created."

PACKAGE_PATH=$(ls -1t "${OUTPUT_DIR}"/*.nupkg 2>/dev/null | head -1)
if [[ -n "${PACKAGE_PATH}" ]]; then
  echo "  $(basename "${PACKAGE_PATH}")"
  echo ""
  echo "Contents:"
  unzip -l "${PACKAGE_PATH}"
fi
