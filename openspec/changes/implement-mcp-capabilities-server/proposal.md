## Why

MCP servers built with the `ModelContextProtocol` C# SDK need to gate tool, prompt, and resource visibility based on the connected client's `ClientCapabilities`. Without a library, developers must manually inspect `ClientCapabilities` in custom `ListToolsHandler` implementations — duplicating boilerplate across every project. This library provides a compile-time attribute-based annotation system with runtime FluentResults-based filtering, eliminating the boilerplate while giving callers structured, composable error information instead of opaque exceptions.

## What Changes

- **NEW** NuGet package `McpCapabilities.Server` in `src/McpCapabilities.Server/`
- **NEW** `[RequiredClientCapabilities]` attribute to annotate MCP tools, prompts, and resources at compile time
- **NEW** `AddCapabilityGating()` builder extension that wraps `ListToolsHandler`, `ListPromptsHandler`, and `ListResourcesHandler` with capability-based filtering
- **NEW** `WithCapabilityAwareTools<T>()` builder extension for registration-time reflection capture into `_meta`
- **NEW** `CapabilityFlag` enum mapping all `ClientCapabilities` features to a `[Flags]` bitmask
- **NEW** `CapabilityNotMetError` — a FluentResults `IError` carrying structured capability-failure data for composable error handling
- **NEW** Extension methods for capability capture/read on `McpServerTool`, `McpServerPrompt`, `McpServerResource` via `Protocol*.Meta`
- **NEW** Unit tests (`tests/McpCapabilities.Server.Unit.Tests/`) and integration tests (`tests/McpCapabilities.Server.Integration.Tests/`) written before implementation (TDD)
- **NEW** Project entry in the solution file (`McpCapabilities.slnx`)

## Capabilities

### New Capabilities

- `capability-flag-enum`: Bitmask enum mapping all MCP `ClientCapabilities` features (Sampling, Roots, Elicitation, Tasks, and their sub-features) for efficient comparison
- `capability-requirements-attribute`: The `[RequiredClientCapabilities]` attribute placed on MCP primitive methods to declare which client capabilities are required
- `meta-storage`: Reading/writing capability requirements to/from `ProtocolTool.Meta`, `ProtocolPrompt.Meta`, and `ProtocolResource.Meta` JsonObject for zero-reflection runtime reads
- `capability-filtering-handlers`: Pre-built handler wrappers that filter `tools/list`, `prompts/list`, and `resources/list` results based on the calling client's `ClientCapabilities`
- `capability-aware-tools-registration`: Builder extension `WithCapabilityAwareTools<T>()` that captures `[RequiredClientCapabilities]` attributes at registration time via one-time reflection
- `add-capability-gating`: The `AddCapabilityGating()` one-liner builder extension that wires all filtering handlers into `McpServerOptions`
- `fluent-results-errors`: `CapabilityNotMetError` (FluentResults `IError`) with structured data for Missing flags, Required flags, and PrimitiveName; enables composable `Result<T>` pipelines in consuming code

### Modified Capabilities

<!-- No existing specs to modify -->

## Impact

- **New source project**: `src/McpCapabilities.Server/McpCapabilities.Server.csproj` targeting `net10.0`
- **New test projects**: Already scaffolded `tests/McpCapabilities.Server.Unit.Tests/` and `tests/McpCapabilities.Server.Integration.Tests/`
- **Solution**: Updated `McpCapabilities.slnx` to include the new server project
- **Dependencies**: `ModelContextProtocol` (NuGet), `FluentResults` (NuGet), `Microsoft.Extensions.DependencyInjection` and `Microsoft.Extensions.Options` (framework)
- **Directory.Packages.props**: Updated with `ModelContextProtocol`, `FluentResults`, and test package versions
- **Naming**: Package IDs use `McpCapabilities` prefix consistent with the existing solution structure (not `ModelContextProtocol.Capabilities` as in the design doc)
