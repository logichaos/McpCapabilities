## Context

The `SampleMcpServer` demonstrates server-side capability gating but has no client-side counterpart to validate the filtering. A sample MCP client (or multiple client configurations) is needed that connects to the server with different `ClientCapabilities` profiles and verifies which primitives are visible.

## Goals / Non-Goals

**Goals:**
- Provide at least two client capability profiles: "full" (Sampling, Roots, Elicitation) and "minimal" (no capabilities)
- Connect each profile to `SampleMcpServer` via stdio transport
- Print the results of `tools/list`, `prompts/list`, and `resources/list` for each profile
- Demonstrate that gated primitives are hidden from the minimal client and visible to the full client

**Non-Goals:**
- A reusable client library — this is a one-shot demonstration script
- Interactive client UI — console output only
- Tests for the client project (manual verification against the sample server)
- Transport types other than stdio

## Decisions

### Decision 1: Single Program.cs with multiple profiles in sequence

**Choice**: One `Program.cs` that creates two `McpClient` instances sequentially, each with different `ClientCapabilities`, and runs the same listing logic against the server.

**Rationale**: Keeps the sample minimal — one file, one `dotnet run` invocation. The sequential approach avoids concurrency complexity. Each client profile connects, lists primitives, prints results, and disconnects before the next one starts.

**Alternatives considered**:
- Multiple separate projects or executables — more files and setup complexity; harder to run in one command.
- Parallel connections — adds async complexity with no benefit for a demonstration.

### Decision 2: Stdio transport with dotnet run

**Choice**: Use `StdioClientTransport` with `Command = "dotnet", Arguments = ["run", "--project", "../SampleMcpServer"]`.

**Rationale**: Stdio is the server's only transport, and `dotnet run` ensures the server is always built against the latest source. The client project is in `samples/SampleMcpClient/` adjacent to `samples/SampleMcpServer/`.

**Alternatives considered**:
- Pre-built server binary — requires a separate build step; fragile.
- SSE/HTTP transport — the sample server doesn't support it.

### Decision 3: Client capability profiles

**Choice**: 
- **Full client**: `Sampling = new()`, `Roots = new()`, `Elicitation = new()` → expects 2 tools, 2 prompts, 2 resources
- **Minimal client**: no capabilities set → expects 1 tool (Echo), 1 prompt (Greeting), 1 resource (AppInfo)

**Rationale**: These two profiles cover the extremes — everything visible vs. only ungated primitives. They directly demonstrate the gating behavior of all three primitive types.

### Decision 4: No project reference to SampleMcpServer

**Choice**: The client project does NOT reference `SampleMcpServer.csproj`. It communicates purely through the MCP protocol via stdio.

**Rationale**: This mirrors real-world usage where client and server are separate processes. It also avoids the compilation issues that come from referencing an executable project.

**Dependencies**: Only `ModelContextProtocol` NuGet package.

## Risks / Trade-offs

- **Server startup timing**: The client may attempt to connect before the server is ready → Mitigation: the MCP client's `CreateAsync` includes timeout and retry logic; if needed, add a brief startup delay or a retry loop.
- **Build coupling**: The client uses `dotnet run --project` to start the server, which means the server must be buildable → Mitigation: documented as a prerequisite in the README.
