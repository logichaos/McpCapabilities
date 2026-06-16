CONFIGURATION ?= Release

.DEFAULT_GOAL := help

.PHONY: help build test test-unit test-integration coverage clean pack all

help:
	@echo "McpCapabilities — quality gate and packaging targets"
	@echo ""
	@echo "Usage: make [target] [CONFIGURATION=Debug|Release]"
	@echo ""
	@echo "Targets:"
	@echo "  build             Compile the solution"
	@echo "  test              Run all tests (unit + integration)"
	@echo "  test-unit         Run unit tests only"
	@echo "  test-integration  Run integration tests only"
	@echo "  coverage          Build, test with coverage, check ≥ 95%"
	@echo "  clean             Remove all artifacts"
	@echo "  pack              Build NuGet package (version from git tags)"
	@echo "  all               Build → test → coverage (full quality gate)"
	@echo "  help              Show this help"
	@echo ""
	@echo "Options:"
	@echo "  CONFIGURATION     Build configuration (default: Release)"
	@echo ""
	@echo "Versioning:"
	@echo "  MinVer derives package version from git tags:"
	@echo "    git tag v1.2.3"
	@echo "    make pack"


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

pack:
	./scripts/pack.sh --configuration $(CONFIGURATION)

all: build test coverage
