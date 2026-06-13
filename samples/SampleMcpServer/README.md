# SampleMcpServer

A minimal MCP server demonstrating the `McpCapabilities.Server` library's capability-gating features.

## What it demonstrates

- **Gated tools** — `AiSummarize` requires the client to support LLM sampling
- **Gated prompts** — `ConfirmAction` requires the client to support user elicitation
- **Gated resources** — `WorkspaceFiles` requires the client to support filesystem roots
- **Ungated primitives** — `Echo`, `Greeting`, and `AppInfo` are always visible

## Primitives

| Type | Name | Required Capability | Description |
|------|------|-------------------|-------------|
| Tool | `AiSummarize` | `Sampling` | Returns a placeholder summary |
| Tool | `Echo` | *(none)* | Returns the input verbatim |
| Prompt | `ConfirmAction` | `Elicitation` | Guides the LLM to elicit user confirmation |
| Prompt | `Greeting` | *(none)* | A simple greeting prompt |
| Resource | `WorkspaceFiles` | `Roots` | Returns workspace file listing |
| Resource | `AppInfo` | *(none)* | Returns server metadata |

## Build

```bash
cd samples/SampleMcpServer
dotnet build
```

## Run

The server supports three transport modes, configurable via `appsettings.json`:

```json
{
  "MCP": {
    "Transport": "stdio"  // "stdio", "http", or "both"
  }
}
```

### stdio mode (default)

```bash
dotnet run
```

The server listens on stdio. Connect with any MCP-compatible client (Claude Desktop, VS Code, etc.).

### http mode

Set `"Transport": "http"` in `appsettings.json`, then:

```bash
dotnet run
```

The server starts an HTTP listener. Connect via HTTP MCP clients at the configured port.

### both mode

Set `"Transport": "both"` in `appsettings.json`, then:

```bash
dotnet run
```

The server listens on both stdio and HTTP simultaneously. A background task handles stdio while the web host handles HTTP.

## How it works

1. `[RequiredClientCapabilities]` attributes are placed on tool/prompt/resource methods.
2. `WithCapabilityAwareTools<T>()` captures tool capability requirements at registration time.
3. `AddCapabilityGating()` captures all primitive requirements, builds filtered handlers, and enables request-time filtering based on the connected client's `ClientCapabilities`.
4. When a client connects without required capabilities, gated primitives are silently hidden.
