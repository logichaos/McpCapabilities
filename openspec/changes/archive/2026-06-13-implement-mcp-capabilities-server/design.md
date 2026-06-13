## Context

The `ModelContextProtocol` C# SDK (`ModelContextProtocol` NuGet package) provides the building blocks for MCP servers: `IMcpServerBuilder`, `McpServerOptions`, `McpServerTool`, `McpServerPrompt`, `McpServerResource`, and handler delegates (`ListToolsHandler`, `ListPromptsHandler`, `ListResourcesHandler`). Each MCP primitive type has a `Protocol*.Meta` property (a `JsonObject`) that survives serialization — ideal for storing metadata.

The connected client's `ClientCapabilities` is available via `McpServer.ClientCapabilities` after the initialization handshake (Section 10 of `McpServerTutorial.md`). The SDK gating design is detailed in `CapabilitiesWrapper.md` Section 3; the FluentResults approach is explored in `CapabilitiesWrapperMonads.md` Section 4.

The existing solution (`McpCapabilities.slnx`) has scaffolded unit and integration test projects under `tests/` using TUnit, central package management (`Directory.Packages.props`), and targets `net10.0`.

## Goals / Non-Goals

**Goals:**
- Provide a `[RequiredClientCapabilities]` attribute for annotating MCP server primitive methods
- Capture capability requirements at registration time via one-time reflection into `Protocol*.Meta`
- Provide `AddCapabilityGating()` builder extension that wraps list handlers with capability filtering
- Return FluentResults `Result<T>` from filtering operations with structured `CapabilityNotMetError` for composable error handling
- Achieve >95% code coverage on the production library (`src/McpCapabilities.Server/`)
- Deliver as a NuGet package using existing solution conventions (CPM, TUnit, `slnx`)

**Non-Goals:**
- Client-side capability gating (`McpCapabilities.Client`) — out of scope for this change
- Shared abstractions package — the `CapabilityFlag` enum lives in the server package directly
- Guarding `tools/call` or `prompts/get` at invocation time (the MCP SDK's built-in checks already handle this; this library focuses on list filtering)
- Authentication/authorization integration (handled by ASP.NET Core middleware separately)

## Decisions

### 1. FluentResults over Custom Result<T>

**Decision**: Use `FluentResults` (NuGet) for the error model.

**Rationale**: Per `CapabilitiesWrapperMonads.md` Section 4, FluentResults provides first-class support for multiple errors per result (`result.Errors` is `List<IError>`), structured metadata via `WithMetadata()`, exception chaining via `CausedBy()`, and built-in logging hooks. The allocation cost (reference type) is acceptable because list filtering is not on a sub-millisecond hot path — it runs once per `tools/list` request. For a library consumed in ASP.NET Core apps where `FluentResults` is already common, the dependency weight is justified.

**Alternatives considered**: Custom `Result<T, CapabilityError>` value-type struct (Approach A) — rejected because it lacks multi-error support, has no built-in logging, and requires consumers to learn a custom API. `OneOf` (Approach B) — rejected because it forces a dependency on a different union-type library, has a different programming model, and lacks the rich error metadata that FluentResults provides.

### 2. `_meta` Storage over Custom DI State

**Decision**: Store capability requirements in `ProtocolTool.Meta`, `ProtocolPrompt.Meta`, `ProtocolResource.Meta` as JsonObject entries.

**Rationale**: This is the SDK's built-in extension point. It survives JSON serialization (clients see it in list responses), requires zero allocation for reads at request time, and avoids polluting the DI container with per-tool state. The well-known key `__mcp_capabilities_required` stores serialized `ClientCapabilityRequirements`.

**Alternatives considered**: Custom `ConcurrentDictionary<McpServerTool, ClientCapabilityRequirements>` in DI — rejected because it couples the filtering handler to DI resolution and doesn't survive serialization. A custom `McpServerTool` subclass — rejected because it requires users to change their tool creation pattern.

### 3. Handler Wrapping over Middleware/Filter

**Decision**: Wrap `ListToolsHandler`, `ListPromptsHandler`, `ListResourcesHandler` delegates in `McpServerOptions.Handlers`.

**Rationale**: Handler wrapping preserves any existing handler chain (e.g., user-provided `ListToolsHandler` from `.WithListToolsHandler()`). It's the pattern already used by the SDK for authorization filters (`.AddAuthorizationFilters()`). The wrapping happens in `AddCapabilityGating()` via `builder.Services.Configure<McpServerOptions>()`.

**Alternatives considered**: `McpServerFilters` (message-level/request-level) — rejected because filters don't have access to the full tool list post-resolution; they intercept at the message boundary, not the handler boundary.

### 4. Registration-Time Capture (One-Time Reflection)

**Decision**: Reflect on `[RequiredClientCapabilities]` attributes at registration/startup time, write results to `_meta`, and never reflect again at request time.

**Rationale**: This is the "compile-time declare, startup-capture, runtime-read" pattern from `CapabilitiesWrapper.md` Section 3.3. The `WithCapabilityAwareTools<T>()` extension uses `IConfigureOptions<McpServerOptions>` (or a post-registration hook) to iterate tools after the DI container is built, capture attributes, and write to `ProtocolTool.Meta`.

**Alternatives considered**: Per-request reflection — rejected for performance. Source generator — viable future enhancement but over-engineered for the initial implementation (the reflection happens once, at startup, for a known small number of tools).

### 5. CapabilityFlag Enum Scope

**Decision**: Define `CapabilityFlag` internal to the server package (not in a shared abstractions package).

**Rationale**: The client-side library is out of scope for this change. When the client library is built, `CapabilityFlag` (with server-side flags) can be split into an abstractions package. For now, keeping it in the server package avoids the overhead of a third project.

### 6. TUnit over xUnit/NUnit

**Decision**: Use TUnit (already in `Directory.Packages.props` at version `1.54.0`).

**Rationale**: The existing solution has already standardized on TUnit with `TUnit.AspNetCore` for integration tests. This is a project convention.

## Risks / Trade-offs

- **FluentResults dependency weight**: Adds ~15 transitive packages. Mitigation: `FluentResults` is widely adopted (100M+ downloads), and many ASP.NET Core apps already use it. The dependency is explicitly declared so consumers can assess the cost.
- **`_meta` key collision**: Another library or user code could overwrite `__mcp_capabilities_required`. Mitigation: Use a namespaced key name unlikely to collide; document the key in the README.
- **Reflection at startup**: `WithCapabilityAwareTools<T>()` uses `MethodInfo.GetCustomAttribute` which is slow for thousands of tools. Mitigation: MCP servers typically register 5–50 tools; this is a one-time startup cost measured in microseconds.
- **No invocation-time guard**: If a client somehow knows a tool name and calls `tools/call` directly (bypassing `tools/list`), the tool will execute even if capabilities aren't met. Mitigation: The MCP SDK already has its own capability guards inside `McpServer.Methods.cs` (e.g., `ThrowIfSamplingUnsupported`). The library's list filtering is additional defense-in-depth; the SDK handles the actual call path.

## Open Questions

- Should the `McpCapabilities.Server` package reference `ModelContextProtocol` or the specific sub-packages (`ModelContextProtocol.Core`, `ModelContextProtocol.AspNetCore`)? Resolution: Reference the `ModelContextProtocol` meta-package, which pulls in all required SDK types.
- Should the `AddCapabilityGating()` automatically call `WithCapabilityAwareTools<T>()` or leave them as separate concerns? Resolution: Keep them separate — `AddCapabilityGating()` wires handlers; `WithCapabilityAwareTools<T>()` captures attributes. Users can use filtering without attribute capture if they populate `_meta` manually.
