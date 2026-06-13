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

```bash
dotnet run
```

The server listens on stdio. Connect with any MCP-compatible client (Claude Desktop, VS Code, etc.).

## How it works

1. `[RequiredClientCapabilities]` attributes are placed on tool/prompt/resource methods.
2. `WithCapabilityAwareTools<T>()` captures tool capability requirements at registration time.
3. A post-configure options class captures prompt and resource capability requirements.
4. `AddCapabilityGating()` wraps the list handlers to filter primitives based on the connected client's `ClientCapabilities`.
5. When a client connects without required capabilities, gated primitives are silently hidden.
