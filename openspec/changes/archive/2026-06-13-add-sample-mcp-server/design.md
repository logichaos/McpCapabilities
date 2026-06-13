## Context

The `McpCapabilities.Server` library provides capability-gating infrastructure but lacks a runnable sample. This design covers a standalone console-hosted MCP server that exercises the library's full surface: `WithCapabilityAwareTools<T>()`, `AddCapabilityGating()`, and `[RequiredClientCapabilities]` on tools, prompts, and resources. The sample should be self-contained, buildable with `dotnet run`, and small enough to serve as a quick-start reference for library users.

## Goals / Non-Goals

**Goals:**
- Demonstrate every primitive type (tool, prompt, resource) with and without `[RequiredClientCapabilities]`
- Show the full hosting pipeline in a single `Program.cs`
- Produce a runnable binary that an MCP client (e.g., Claude Desktop, VS Code) can connect to via stdio
- Keep the sample minimal — no external databases, no cloud dependencies

**Non-Goals:**
- Transport types other than stdio (no SSE, no WebSocket)
- Authentication, authorization, or rate-limiting
- Dynamic capability registration at runtime
- A test suite for the sample project (the library's own tests sufficiently exercise the gating logic)

## Decisions

### Decision 1: Single project under `samples/` directory

**Choice**: `samples/SampleMcpServer/SampleMcpServer.csproj` with a project reference to `src/McpCapabilities.Server`.

**Rationale**: Keeps the sample adjacent to the library source while clearly separating library code from example code. Using a project reference (not a NuGet package reference) ensures the sample always builds against the latest local source.

**Alternatives considered**:
- NuGet package reference — would require publishing the library before the sample can be built; not suitable for inner-loop development.
- Inline in `src/` — muddies the library with sample code and could confuse coverage tooling.

### Decision 2: Three primitive classes, one per type

**Choice**: Separate classes `AiTools`, `HelpfulPrompts`, and `WorkspaceResources`, each with at least two methods (one gated, one ungated).

**Rationale**: Mirrors the real-world pattern where tools, prompts, and resources live in separate `[McpServerToolType]`, `[McpServerPromptType]`, `[McpServerResourceType]` classes. Each class demonstrates a different capability flag (`Sampling`, `Elicitation`, `Roots`) so users see the annotation syntax for all three scenarios.

**Alternatives considered**:
- Single class with all three types — possible but not idiomatic with the MCP SDK; each `McpServerTool` etc. requires its own marker attribute on the containing type.
- Multiple tools sharing the same flag — provides less coverage of the capability enum.

### Decision 3: Stdio transport only

**Choice**: Use `WithStdioServerTransport()` in `Program.cs` for the simplest possible setup.

**Rationale**: Stdio is the default transport for MCP servers and the easiest to test with any MCP client. No port configuration or network setup needed.

### Decision 4: All primitive logic is stubs (logging-only)

**Choice**: Tools, prompts, and resources will log their invocation but return simple placeholder results. No real LLM calls, no filesystem access.

**Rationale**: The sample's purpose is to demonstrate capability gating, not to implement useful primitives. Stubs keep the sample short and make it obvious that the focus is on the gating behavior.

## Risks / Trade-offs

- **Sample becomes outdated if the library API changes** → Mitigation: the sample uses a project reference, so any API breakage will cause a build failure. A CI pipeline that builds the sample alongside the library would catch this.
- **Users may think this is a production template** → Mitigation: the `README.md` in the sample directory will clearly state it's a demonstration only.
