# McpCapabilities

**Capability-gating library for MCP servers.** Annotate your tools, prompts, and resources with `[RequiredClientCapabilities]` — the library automatically hides them from clients that don't advertise the required capabilities.

```mermaid
flowchart LR
    subgraph Server
        A[Tool: Summarize<br/>requires Sampling]
        B[Tool: Echo<br/>no requirements]
    end
    subgraph "Client A<br/>(has Sampling)"
        A1[✓ Summarize visible]
        A2[✓ Echo visible]
    end
    subgraph "Client B<br/>(no Sampling)"
        B1[✗ Summarize hidden]
        B2[✓ Echo visible]
    end
    A --> A1
    B --> A2
    A -.->|filtered| B1
    B --> B2
```

## Why?

MCP servers often expose features that depend on client-side capabilities — LLM sampling, user elicitation, filesystem roots, etc. Without capability gating, every client sees every tool, even ones it can't use. That leads to broken UX and confusing error messages.

`McpCapabilities.Server` lets you declare what each tool/prompt/resource needs, and the library handles the filtering at runtime — silently hiding unavailable primitives from under-capable clients.

## Installation

```bash
dotnet add package McpCapabilities.Server
```

Dependencies: `ModelContextProtocol` (≥1.4.0), `FluentResults` (≥4.0.0).

## Quick Start

### 1. Annotate your server primitives

```csharp
using McpCapabilities.Server;
using ModelContextProtocol.Server;

[McpServerToolType]
public class MyTools
{
    [McpServerTool]
    [RequiredClientCapabilities(
        Required = CapabilityFlag.Sampling,
        Message = "This tool requires LLM sampling support")]
    public async Task<string> Summarize(
        McpServer server,
        string text,
        CancellationToken ct)
    {
        var result = await server.SampleAsync(/* ... */, cancellationToken: ct);
        return result.Content.OfType<TextContentBlock>().First().Text;
    }

    [McpServerTool]
    public string Echo(string text) => text; // always visible
}
```

### 2. Register with capability gating

```csharp
builder.Services.AddMcpServer()
    .WithTools<MyTools>()       // register annotated tools
    .AddCapabilityGating();     // enable runtime filtering
```

That's it. Clients without sampling capability won't see the `Summarize` tool.

## How It Works

```mermaid
sequenceDiagram
    participant Dev as Developer
    participant Reg as Registration
    participant Meta as Tool Metadata
    participant Client as MCP Client
    participant Svr as MCP Server
    participant Filter as Capability Filter

    Dev->>Reg: Adds [RequiredClientCapabilities]<br/>to tool methods
    Reg->>Meta: Captures requirements into<br/>ProtocolTool.Meta JSON
    Note over Meta: {"__mcp_capabilities_required":<br/>  {"flags": "Sampling"}}

    Client->>Svr: Initialize (sends ClientCapabilities)
    Svr->>Filter: Stores client's capabilities

    Client->>Svr: tools/list
    Svr->>Filter: Wraps list handler
    Filter->>Filter: For each tool:<br/>(clientFlags & required) == required?
    Filter->>Client: Returns only compatible tools
```

The two-phase architecture:

```mermaid
stateDiagram-v2
    [*] --> Annotated: Developer adds<br/>[RequiredClientCapabilities]
    Annotated --> Captured: WithTools() registration<br/>(reflection, one-time)
    Captured --> Stored: Requirements written<br/>to Protocol*.Meta
    Stored --> Waiting: Server ready
    Waiting --> Filtered: Client requests list
    Filtered --> Visible: Client has all<br/>required capabilities
    Filtered --> Hidden: Client lacks<br/>required capabilities
    Visible --> [*]
    Hidden --> [*]
```

## Capability Flags

`CapabilityFlag` is a `[Flags]` enum. Combine multiple flags with bitwise OR.

| Flag | Built-in Capability | Description |
|------|-------------------|-------------|
| `None` | — | No capabilities required |
| `Sampling` | `ClientCapabilities.Sampling` | LLM sampling requests |
| `Roots` | `ClientCapabilities.Roots` | Filesystem root listing |
| `Elicitation` | `ClientCapabilities.Elicitation` | Elicitation (any mode) |
| `ElicitationForm` | `Elicitation.Form` | Form-mode elicitation |
| `ElicitationUrl` | `Elicitation.Url` | URL-mode elicitation |
| `Tasks` | `ClientCapabilities.Tasks` | Task-augmented requests |
| `TaskList` | `Tasks.List` | Task listing |
| `TaskCancel` | `Tasks.Cancel` | Task cancellation |
| `TaskAugmentedSampling` | `Tasks.Requests.Sampling` | Task-augmented LLM sampling |
| `TaskAugmentedElicitation` | `Tasks.Requests.Elicitation` | Task-augmented elicitation |

### Bitmask Satisfaction

```mermaid
graph TB
    subgraph "✓ Satisfied"
        C1["Client has: Sampling, Roots, Elicitation<br/><code>flags = 00111</code>"]
        R1["Tool requires: Sampling | Elicitation<br/><code>required = 00101</code>"]
        M1["<code>(00111 & 00101) == 00101</code> ✓"]
    end
    subgraph "✗ Not Satisfied"
        C2["Client has: Sampling only<br/><code>flags = 00001</code>"]
        R2["Tool requires: Sampling | Elicitation<br/><code>required = 00101</code>"]
        M2["<code>(00001 & 00101) != 00101</code> ✗"]
    end
```

## Usage Patterns

### Gating tools

```csharp
[McpServerToolType]
public class AiTools
{
    [McpServerTool]
    [RequiredClientCapabilities(Required = CapabilityFlag.Sampling)]
    public async Task<string> AiSummarize(McpServer server, string text, CancellationToken ct)
    {
        var result = await server.SampleAsync(
            new CreateMessageRequestParams { /* ... */ },
            cancellationToken: ct);
        return result.Content.OfType<TextContentBlock>().First().Text;
    }

    [McpServerTool]
    [RequiredClientCapabilities(Required = CapabilityFlag.Elicitation)]
    public async Task<string> AiChoose(McpServer server, string options, CancellationToken ct)
    {
        var result = await server.ElicitAsync(/* ... */, cancellationToken: ct);
        return $"Chose: {result.Content}";
    }

    [McpServerTool]
    [RequiredClientCapabilities(
        Required = CapabilityFlag.Sampling | CapabilityFlag.Elicitation,
        Message = "This tool requires both LLM sampling and user elicitation")]
    public async Task<string> AdvancedAnalysis(McpServer server, CancellationToken ct)
    {
        var sample = await server.SampleAsync(/* ... */, ct);
        var elicit = await server.ElicitAsync(/* ... */, ct);
        return Process(sample, elicit);
    }

    [McpServerTool]
    public string Echo(string text) => text;
}
```

### Gating prompts

```csharp
[McpServerPromptType]
public class HelpfulPrompts
{
    [McpServerPrompt]
    [RequiredClientCapabilities(Required = CapabilityFlag.Elicitation)]
    public string ConfirmAction() =>
        "Ask the user to confirm before proceeding. Use elicitation to collect their choice.";

    [McpServerPrompt]
    public string Greeting() =>
        "Greet the user warmly and ask how you can help them today.";
}
```

### Gating resources

```csharp
[McpServerResourceType]
public class WorkspaceResources
{
    [McpServerResource]
    [RequiredClientCapabilities(Required = CapabilityFlag.Roots)]
    public string WorkspaceFiles() => "file:///workspace/**";

    [McpServerResource]
    public string AppInfo() => "SampleMcpServer v1.0";
}
```

### Registration

```csharp
builder.Services.AddMcpServer(options =>
{
    options.ServerInfo = new Implementation
    {
        Name = "MyServer",
        Version = "1.0",
    };
})
    .WithTools<AiTools>()
    .WithPrompts<HelpfulPrompts>()
    .WithResources<WorkspaceResources>()
    .AddCapabilityGating();
```

### Programmatic filtering (FluentResults)

```csharp
using McpCapabilities.Server;
using FluentResults;

var tools = GetFullToolList();
var clientCaps = GetConnectedClientCapabilities();

var result = tools.FilterByClientCapabilities(clientCaps);

result.Switch(
    success: visible =>
    {
        foreach (var tool in visible)
            Console.WriteLine($"  - {tool.Name}");
    },
    failure: errors =>
    {
        var error = errors.OfType<CapabilityNotMetError>().First();
        Console.WriteLine($"Client lacks: {error.Missing}");
    });
```

## Versioning

This project uses [MinVer](https://github.com/adamralph/minver) for automatic versioning from git tags:

```bash
# Cut a release
git tag v1.2.3

# Package picks up the version automatically
make pack      # → McpCapabilities.Server.1.2.3.nupkg
```

Commits after a tag get a pre-release suffix (e.g., `1.2.4-alpha.0.5`).

## Documentation

- [Library docs](src/McpCapabilities.Server/README.md) — detailed API reference
- [Sample server](samples/SampleMcpServer/README.md) — walkthrough of the included sample
- [Development guide](DEVELOPMENT.md) — build, test, package, and publish instructions

## License

MIT
