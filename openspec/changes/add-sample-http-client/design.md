## Context

The `SampleMcpServer` already supports HTTP transport (via `WithHttpTransport()` + `app.MapMcp()`) and the existing `SampleMcpClient` connects only via stdio. This leaves the HTTP transport path untested at the integration level and undocumented for client developers. The MCP SDK provides HTTP-based transport types (e.g., `HttpClientSseTransport`) suitable for a client to connect to an MCP server over HTTP.

This change adds a mirror of the stdio client sample (same capability profile logic) but over HTTP transport, plus an integration test project that hosts the server in-process and verifies capability-gated filtering across the wire.

## Goals / Non-Goals

**Goals:**
- Provide a working sample of an MCP HTTP client that connects to `SampleMcpServer`
- Add integration tests that start the server in HTTP mode, connect a client, and verify capability gating works end-to-end over HTTP
- Follow the same project conventions (Central Package Management, TUnit, file-scoped namespaces, nullable enable)

**Non-Goals:**
- No changes to the existing stdio client or server
- No new transport protocol implementation
- No new capability flags or filtering logic
- No Docker or container-based testing

## Decisions

### Decision 1: Client transport — use `SseClientTransport` or the SDK's HTTP client transport

The MCP SDK (v1.4.0+) provides HTTP-based client transport options. We will use the appropriate SDK-provided HTTP transport type that handles the MCP SSE or HTTP streaming protocol. Based on the server's `app.MapMcp()` setup, the client transport connects to `<baseUrl>/mcp`.

**Alternatives considered:**
- Writing a custom HTTP transport: Not needed; the SDK already handles this.
- Using the stdio client and spawning the server as a subprocess with `--urls`: This would test stdio, not HTTP; the goal is to test the HTTP transport path.

### Decision 2: Integration test hosting — use `WebApplicationFactory`-style in-process hosting

The integration tests will programmatically create a `WebApplication` (as in the existing `DualTransportTests`) configured for HTTP-only mode, with `UseUrls("http://127.0.0.1:0")` for a random port. The server is started as a background task, the client connects, and assertions are made.

**Alternatives considered:**
- Spawning the server as a separate process: Adds complexity, port management, and startup race conditions.
- Using `Microsoft.AspNetCore.Mvc.Testing`: Overkill for this; a simple `WebApplication` builder with a background Task.Run is sufficient and matches existing patterns in `DualTransportTests`.

### Decision 3: Test framework — TUnit with `TUnit.AspNetCore`

The project follows the existing `SampleMcpServer.Integration.Tests` pattern: `Microsoft.NET.Sdk.Web`, TUnit, and `TUnit.AspNetCore`. Tests are async methods returning `Task`.

### Decision 4: Project structure — separate sample and separate test project

Mirroring the existing layout:
- `samples/SampleMcpHttpClient/` — console app
- `tests/SampleMcpHttpClient.Integration.Tests/` — integration tests

This keeps the HTTP client sample independent from the stdio client and avoids coupling their builds.

### Decision 5: Capability profiles — reuse the same two profiles

The HTTP client runs the same two profiles as the stdio client:
1. **Full**: Sampling + Roots + Elicitation → expects all 6 primitives
2. **Minimal**: No capabilities → expects 3 ungated primitives

This enables direct comparison between transports and ensures filtering is transport-agnostic (as already specified in `both-transport-mode`).

## Risks / Trade-offs

- **SDK transport API may change**: The MCP SDK HTTP client transport API could evolve across versions. → Pin the `ModelContextProtocol` package version via CPM, same as the rest of the solution.
- **Port conflicts in CI**: Random port assignment may theoretically fail. → Use `http://127.0.0.1:0` which asks the OS for a free port; conflicts are extremely rare.
- **Test timing**: Async startup of the web host before client connect may need coordination. → Use a loop or `Task.Delay` to wait for the server to be ready, or check the bound port before creating the client transport.
- **No streaming test**: This change does not test tool invocation or prompt retrieval over HTTP — only listing. → Out of scope; the goal is to verify primitive visibility via capability gating, which only requires list operations.
