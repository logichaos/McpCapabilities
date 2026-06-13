## Why

The `McpCapabilities.Server` library provides capability-gating infrastructure, but there is no runnable sample demonstrating how to use it end-to-end. A sample server project is needed so that library users can see concrete, working examples of annotated tools, prompts, and resources — and so that the project can validate the library's integration points in a realistic scenario.

## What Changes

- **NEW** project `samples/SampleMcpServer/SampleMcpServer.csproj` — a console-hosted MCP server that exercises all three primitive types (tools, prompts, resources) with `[RequiredClientCapabilities]` attributes
- **NEW** `SampleMcpServer/Program.cs` — hosting setup with `AddMcpServer().WithCapabilityAwareTools<T>().AddCapabilityGating()`
- **NEW** `SampleMcpServer/AiTools.cs` — at least two tools: one gated on `CapabilityFlag.Sampling`, one always visible
- **NEW** `SampleMcpServer/HelpfulPrompts.cs` — at least two prompts: one gated (e.g., on `CapabilityFlag.Elicitation`), one always visible
- **NEW** `SampleMcpServer/WorkspaceResources.cs` — at least two resources: one gated (e.g., on `CapabilityFlag.Roots`), one always visible
- **NEW** `samples/SampleMcpServer/README.md` — brief instructions on how to build and run the sample

## Capabilities

### New Capabilities

- `sample-tools`: A sample MCP tool class with capability-gated and un-gated tools, demonstrating the `Sampling` capability requirement
- `sample-prompts`: A sample MCP prompt class with capability-gated and un-gated prompts, demonstrating the `Elicitation` capability requirement
- `sample-resources`: A sample MCP resource class with capability-gated and un-gated resources, demonstrating the `Roots` capability requirement
- `sample-hosting`: The sample server hosting pipeline wiring together `WithCapabilityAwareTools<T>()` for each primitive type and `AddCapabilityGating()` for request-time filtering

### Modified Capabilities

<!-- No existing specs to modify -->

## Impact

- **New project**: `samples/SampleMcpServer/SampleMcpServer.csproj` targeting `net10.0`
- **Dependencies**: `McpCapabilities.Server` (project reference), `ModelContextProtocol` (for server hosting)
- **Solution**: `McpCapabilities.slnx` updated to include the sample project (or a separate `samples/SampleMcpServer.slnx`)
- **No breaking changes** — the library's public API is unchanged
- **No new NuGet packages** — this is a sample, not a library
