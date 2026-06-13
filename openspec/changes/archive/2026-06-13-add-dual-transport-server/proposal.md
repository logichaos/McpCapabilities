## Why

The sample server currently only supports stdio transport, limiting it to desktop MCP clients like Claude Desktop. Adding HTTP transport support and the ability to switch between transports via configuration enables the sample to be used with web-based MCP clients, remote connections, and load-balanced deployments — all without code changes.

## What Changes

- **MODIFIED** `samples/SampleMcpServer/SampleMcpServer.csproj` — switch from console app to web app (`Microsoft.NET.Sdk.Web`)
- **MODIFIED** `samples/SampleMcpServer/Program.cs` — replace `Host.CreateApplicationBuilder` with `WebApplication.CreateBuilder`, add `WithHttpTransport()` conditionally, add `app.MapMcp()`, support stdio mode in a background task for "both"
- **NEW** `samples/SampleMcpServer/appsettings.json` — configuration file with `"MCP": { "Transport": "stdio" }` (valid values: `"stdio"`, `"http"`, `"both"`)
- **MODIFIED** `samples/SampleMcpServer/README.md` — document the three transport modes and how to configure them

## Capabilities

### New Capabilities

- `dual-transport-configuration`: The server reads transport mode from `appsettings.json` at startup and configures transports accordingly
- `http-transport`: The server supports HTTP transport via `WithHttpTransport()` + `MapMcp()`, making it accessible to web-based MCP clients
- `both-transport-mode`: When configured as `"both"`, the server serves stdio in a background task while the web host handles HTTP requests on the same `IMcpServer` instance

### Modified Capabilities

<!-- No existing specs to modify -->

## Impact

- **Project type change**: `SampleMcpServer.csproj` switches from `Microsoft.NET.Sdk` to `Microsoft.NET.Sdk.Web`
- **New dependency**: Implicit `Microsoft.AspNetCore.App` framework reference (already available via `Sdk.Web`)
- **No breaking changes**: Default transport remains `"stdio"`; existing behavior unchanged
- **Configuration**: New `appsettings.json` file with `MCP:Transport` key
- **No library changes**: `McpCapabilities.Server` is unaffected
