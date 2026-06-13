# ── McpCapabilities Makefile ──────────────────────────────────────────────────
# Quality gate targets: build, test, test-unit, test-integration, coverage.
#
# Quick reference:
#   make build             Compile the solution
#   make test              Run all tests (unit + integration)
#   make test-unit         Run unit tests only
#   make test-integration  Run integration tests only
#   make coverage          Build, test with coverage, check ≥ 95% threshold
#   make clean             Remove all artifacts
#   make all               Build → test → coverage (full quality gate)
#
# Options (pass to any target):
#   CONFIGURATION=Release  Build/test configuration (default: Release)

CONFIGURATION ?= Release

.PHONY: build test test-unit test-integration coverage clean all

build:
	./scripts/build.sh --configuration $(CONFIGURATION)

test:
	./scripts/test.sh --configuration $(CONFIGURATION)

test-unit:
	./scripts/test-unit.sh --configuration $(CONFIGURATION)

test-integration:
	./scripts/test-integration.sh --configuration $(CONFIGURATION)

coverage:
	./scripts/coverage.sh --configuration $(CONFIGURATION)

clean:
	./scripts/clean.sh

all: build test coverage
