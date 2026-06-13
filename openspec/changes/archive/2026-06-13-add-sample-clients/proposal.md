## Why

The `SampleMcpServer` demonstrates server-side capability gating, but there is no client-side counterpart to validate that filtering actually works end-to-end. Adding sample MCP clients with different `ClientCapabilities` configurations provides a concrete way to verify that the server correctly hides or shows tools, prompts, and resources based on the client's advertised capabilities.

## What Changes

- **NEW** project `samples/SampleMcpClient/SampleMcpClient.csproj` — a console-hosted MCP client that connects to `SampleMcpServer` via stdio
- **NEW** `samples/SampleMcpClient/Program.cs` — multiple client configurations, each with different `ClientCapabilities`, printing the results of `tools/list`, `prompts/list`, and `resources/list`
- **NEW** `samples/SampleMcpClient/README.md` — instructions for building and running the client against the sample server
- **NEW** at least two client capability profiles: a "full" client with all capabilities (Sampling, Roots, Elicitation) and a "minimal" client with no capabilities

## Capabilities

### New Capabilities

- `sample-client-hosting`: Client builder that configures `ClientCapabilities` and connects to the sample server via stdio transport
- `sample-client-capability-profiles`: Multiple client instances with different `ClientCapabilities` sets, each listing primitives and asserting which should be visible

### Modified Capabilities

<!-- No existing specs to modify -->

## Impact

- **New project**: `samples/SampleMcpClient/SampleMcpClient.csproj` targeting `net10.0`
- **Dependencies**: `ModelContextProtocol` (for MCP client), `Microsoft.Extensions.Hosting` (for hosting)
- **Solution**: `McpCapabilities.slnx` updated to include the client project
- **No breaking changes** — the library and sample server are unchanged
- **No new NuGet packages** — this is a sample, not a library
