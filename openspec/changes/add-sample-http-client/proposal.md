## Why

The sample MCP client currently only connects via stdio transport, but the sample server already supports HTTP and dual-transport modes. There is no client-side example or integration test demonstrating HTTP-based MCP communication with capability gating, leaving a gap in both documentation and test coverage.

## What Changes

- Add a new `SampleMcpHttpClient` sample that connects to the `SampleMcpServer` over HTTP transport (using `SseClientTransport` or equivalent HTTP-based transport from the MCP SDK), running the same two capability profiles (full and minimal) as the stdio client.
- Add a new integration test project (`SampleMcpHttpClient.Integration.Tests`) that starts the sample server in HTTP mode, connects the HTTP client, and asserts that capability-gated tools, prompts, and resources are correctly filtered based on client capabilities.
- Add the new projects to the solution file.
- Update `Directory.Packages.props` if the HTTP client transport requires any additional NuGet dependencies.

## Capabilities

### New Capabilities

- `sample-http-client-hosting`: The HTTP client sample project sets up an MCP client using HTTP-based transport, connects to the server over HTTP, and runs both full-capability and minimal-capability profiles.
- `sample-http-client-integration-tests`: Integration tests that start SampleMcpServer in HTTP mode on a random port, create an HTTP-based MCP client, connect, and assert capability-gated filtering behavior across tools, prompts, and resources.

### Modified Capabilities

<!-- No existing capability specs are modified by this change. This is purely additive. -->

## Impact

- **New project**: `samples/SampleMcpHttpClient/` — a console app referencing `ModelContextProtocol` and using HTTP transport.
- **New test project**: `tests/SampleMcpHttpClient.Integration.Tests/` — integration tests with TUnit, referencing the server sample and the MCP library.
- **Solution file**: `McpCapabilities.slnx` gains two new project entries.
- **No changes** to the existing stdio client, server, or library code.
- **No breaking changes**.
