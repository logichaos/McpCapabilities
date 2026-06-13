## ADDED Requirements

### Requirement: Integration test starts server in HTTP mode on a random port
Each integration test SHALL start the `SampleMcpServer` as a full ASP.NET Core web host in HTTP-only mode, bound to a random available port (e.g., `http://127.0.0.1:0`), so tests are isolated and repeatable.

#### Scenario: Server starts on random port
- **WHEN** the test creates a `WebApplication` host for `SampleMcpServer` configured with `MCP:Transport=http` and `UseUrls("http://127.0.0.1:0")`
- **THEN** the server starts and listens on an OS-assigned port

#### Scenario: Server shuts down cleanly after test
- **WHEN** the test completes and disposes the service provider
- **THEN** the server process terminates without unhandled exceptions

### Requirement: Integration test creates an HTTP client and connects to the running server
The test SHALL create an MCP client using HTTP transport, targeting the server's actual bound port, and connect successfully.

#### Scenario: Client connects to test server
- **WHEN** the test creates an HTTP transport pointing to `http://127.0.0.1:<random-port>/mcp`
- **THEN** `McpClient.CreateAsync` returns a connected client without errors

### Requirement: Full-capability client over HTTP sees all primitives
A test case with a client advertising `Sampling`, `Roots`, and `Elicitation` via HTTP SHALL receive all 6 primitives from the server.

#### Scenario: Full client lists all tools over HTTP
- **WHEN** the full-capability HTTP client calls `ListToolsAsync()`
- **THEN** the result contains both `ai_summarize` and `echo`

#### Scenario: Full client lists all prompts over HTTP
- **WHEN** the full-capability HTTP client calls `ListPromptsAsync()`
- **THEN** the result contains both `confirm_action` and `greeting`

#### Scenario: Full client lists all resources over HTTP
- **WHEN** the full-capability HTTP client calls `ListResourcesAsync()`
- **THEN** the result contains both `workspace_files` and `app_info`

### Requirement: Minimal-capability client over HTTP sees only ungated primitives
A test case with a client advertising no capabilities via HTTP SHALL receive only the ungated primitives.

#### Scenario: Minimal client lists only ungated tools over HTTP
- **WHEN** the minimal-capability HTTP client calls `ListToolsAsync()`
- **THEN** the result contains only `echo` and does NOT contain `ai_summarize`

#### Scenario: Minimal client lists only ungated prompts over HTTP
- **WHEN** the minimal-capability HTTP client calls `ListPromptsAsync()`
- **THEN** the result contains only `greeting` and does NOT contain `confirm_action`

#### Scenario: Minimal client lists only ungated resources over HTTP
- **WHEN** the minimal-capability HTTP client calls `ListResourcesAsync()`
- **THEN** the result contains only `app_info` and does NOT contain `workspace_files`

### Requirement: Capability filtering is consistent across HTTP and stdio
The same capability-based filtering rules SHALL apply over HTTP as over stdio — transport MUST NOT affect which primitives are visible.

#### Scenario: HTTP and stdio produce identical results for full client
- **WHEN** a full-capability client connects via HTTP
- **THEN** it sees the same set of tools, prompts, and resources as a full-capability stdio client

#### Scenario: HTTP and stdio produce identical results for minimal client
- **WHEN** a minimal-capability client connects via HTTP
- **THEN** it sees the same set of tools, prompts, and resources as a minimal-capability stdio client

### Requirement: Integration test project targets Web SDK and uses TUnit
The integration test project SHALL use `Microsoft.NET.Sdk.Web` (to host the server in-process) and `TUnit.AspNetCore` for test infrastructure.

#### Scenario: Test project references are correct
- **WHEN** inspecting the test project file
- **THEN** it references `ModelContextProtocol`, `TUnit`, `TUnit.AspNetCore`, and the sample server project
