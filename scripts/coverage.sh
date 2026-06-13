#!/usr/bin/env bash
set -euo pipefail

# ── Code Coverage ────────────────────────────────────────────────────────────
# Runs tests with code coverage collection and generates a summary report.
#
# Uses Microsoft.Testing.Platform's built-in --coverage option (dotnet-coverage).
#
# Pipeline:
#   1. dotnet test -- --coverage                        → produces *.coverage files
#   2. dotnet-coverage merge -f cobertura               → coverage.cobertura.xml
#   3. reportgenerator                                  → HTML report
#   4. Threshold check (≥ 95 %) via Cobertura XML
#
# Usage: ./scripts/coverage.sh [--configuration <Debug|Release>] [--threshold <pct>]

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(dirname "$SCRIPT_DIR")"
CONFIGURATION="Release"
COVERAGE_DIR="${REPO_ROOT}/artifacts/coverage"
THRESHOLD=95

while [[ $# -gt 0 ]]; do
  case "$1" in
    --configuration|-c) CONFIGURATION="$2"; shift 2 ;;
    --threshold|-t)     THRESHOLD="$2";       shift 2 ;;
    *) echo "Unknown option: $1"; exit 1 ;;
  esac
done

echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "Collecting code coverage (${CONFIGURATION})"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"

# Clean previous coverage artifacts
rm -rf "${COVERAGE_DIR}"
mkdir -p "${COVERAGE_DIR}"

# ── Step 1: Collect coverage ─────────────────────────────────────────────────
echo ""
echo "▸ Running tests with coverage collection…"
dotnet test McpCapabilities.slnx \
  --configuration "${CONFIGURATION}" \
  --results-directory "${COVERAGE_DIR}/raw" \
  -- \
  --no-progress \
  --coverage \
  --coverage-settings "${REPO_ROOT}/coverage.config" \
  --coverage-output-format coverage \
  --ignore-exit-code "5;8"

# ── Step 2: Merge + convert to Cobertura ─────────────────────────────────────
echo ""
echo "▸ Merging coverage files into Cobertura…"
COBERTURA="${COVERAGE_DIR}/coverage.cobertura.xml"
COVERAGE_FILES=$(find "${COVERAGE_DIR}/raw" -name "*.coverage" -type f | tr '\n' ' ')

if [[ -z "${COVERAGE_FILES}" ]]; then
  echo "⚠  No .coverage files produced. The project may have no tests yet."
  echo "   Skipping coverage report generation."
  exit 0
fi

dotnet dotnet-coverage merge ${COVERAGE_FILES} \
  --output "${COBERTURA}" \
  --output-format cobertura

# ── Step 3: Generate HTML report ─────────────────────────────────────────────
echo ""
echo "▸ Generating HTML report…"
HTML_DIR="${COVERAGE_DIR}/report"
dotnet reportgenerator \
  -reports:"${COBERTURA}" \
  -targetdir:"${HTML_DIR}" \
  -reporttypes:Html \
  -verbosity:Warning

# ── Step 4: Check threshold (parse Cobertura XML) ────────────────────────────
echo ""
echo "▸ Checking coverage threshold (≥ ${THRESHOLD}%)…"

# Cobertura XML has <coverage line-rate="X.XX" ...> on the root element.
LINE_RATE=$(grep -oP '<coverage[^>]*\sline-rate="\K[^"]+' "${COBERTURA}" | head -1)

if [[ -z "${LINE_RATE}" ]]; then
  echo "⚠  Could not parse line-rate from Cobertura XML. Check the HTML report:"
  echo "   file://${HTML_DIR}/index.html"
else
  # line-rate is a decimal (0.95 = 95%)
  LINE_COV=$(echo "${LINE_RATE} * 100" | bc -l | xargs printf "%.2f")
  echo "   Line coverage: ${LINE_COV}%"

  if (( $(echo "${LINE_COV} < ${THRESHOLD}" | bc -l) )); then
    echo ""
    echo "✗ Coverage ${LINE_COV}% is below the required ${THRESHOLD}% threshold."
    echo "   Add missing tests before merging."
    exit 1
  else
    echo "   ✓ Coverage meets the ${THRESHOLD}% threshold."
  fi
fi

echo ""
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "Coverage report: file://${HTML_DIR}/index.html"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
