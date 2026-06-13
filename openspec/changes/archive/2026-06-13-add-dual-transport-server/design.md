## Context

The sample server currently uses `Host.CreateApplicationBuilder` and `WithStdioServerTransport()`, limiting it to stdio-only MCP clients. The MCP SDK supports HTTP transport via `WebApplication.CreateBuilder`, `WithHttpTransport()`, and `MapMcp()`. Switching the project to `Sdk.Web` and making transport configurable enables all three transport modes without duplicating the codebase.

## Goals / Non-Goals

**Goals:**
- Support three transport modes: `stdio`, `http`, `both`
- Read transport mode from `appsettings.json` (`MCP:Transport` key)
- Default to `stdio` for backward compatibility
- Single `IMcpServer` instance shared across transports in `both` mode
- Minimal code change to Program.cs

**Non-Goals:**
- HTTP authentication, CORS, or TLS configuration
- Per-client capability filtering differences between transports
- Environment-variable-based transport selection (though it naturally works via ASP.NET config binding)
- Multi-instance server (one `IMcpServer` per transport)

## Decisions

### Decision 1: Switch to `Microsoft.NET.Sdk.Web`

**Choice**: Change the project SDK from `Microsoft.NET.Sdk` to `Microsoft.NET.Sdk.Web` and use `WebApplication.CreateBuilder`.

**Rationale**: `WithHttpTransport()` and `MapMcp()` require ASP.NET Core's web host. `WebApplication.CreateBuilder` is the minimal host for this. The SDK switch also brings `Microsoft.AspNetCore.App` framework reference implicitly.

**Alternatives considered**:
- Keep `Microsoft.NET.Sdk` and add `Microsoft.AspNetCore.App` framework reference manually — works but loses `WebApplication.CreateBuilder` convenience and `appsettings.json` auto-loading.
- Use `SlimBuilder` — insufficient; needs full web host.

### Decision 2: Configuration via `appsettings.json`

**Choice**: `builder.Configuration.GetValue<string>("MCP:Transport")` reading from `appsettings.json`.

**Rationale**: ASP.NET's `WebApplication.CreateBuilder` auto-loads `appsettings.json`. No custom parsing needed. The key path `MCP:Transport` is descriptive and nestable under an `MCP` section.

**Alternatives considered**:
- Command-line args (`--transport http`) — works but requires remembering flags; JSON is more familiar for web apps.
- Environment variables (`MCP__TRANSPORT`) — works as a fallback via the default ASP.NET config provider but is less discoverable.

### Decision 3: Stdio in background task for "both" mode

**Choice**: When `"both"` is configured, launch stdio via `Task.Run(() => server.RunAsync())` alongside `app.Run()` for HTTP.

**Rationale**: The `IMcpServer.RunAsync()` call for stdio blocks on stdin. Running it in a background task lets the HTTP host (`app.Run()`) stay alive. Both share the same `IMcpServer` instance from DI, so capability gating and primitive registration work identically.

**Alternatives considered**:
- Two separate `IMcpServer` instances — doubles memory and registration cost; no benefit for a sample.
- `McpServer.RunAsync()` as the main loop with HTTP in background — `app.Run()` must be the foreground task for graceful shutdown via Ctrl+C.

## Risks / Trade-offs

- **Stdio transport in "both" mode may interfere with HTTP** → Mitigation: tested; the MCP SDK's `McpServer` supports concurrent transport usage. The stdio background task reads from stdin independently of the HTTP request pipeline.
- **`Sdk.Web` pulls in more dependencies** → Mitigation: the sample is already a demonstration project; increased binary size is acceptable.
- **"both" mode shutdown**: Ctrl+C stops `app.Run()` first; the stdio background task may not get a clean shutdown signal → Mitigation: accept brief log noise on exit; production apps would use separate processes.
