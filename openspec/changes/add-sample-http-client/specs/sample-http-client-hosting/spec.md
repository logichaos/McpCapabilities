## ADDED Requirements

### Requirement: Client connects to server over HTTP transport
The HTTP client SHALL use an HTTP-based transport (e.g., `HttpClientSseTransport` or equivalent from the MCP SDK) to connect to the sample server's HTTP endpoint rather than spawning it as a stdio subprocess.

#### Scenario: Client connects over HTTP
- **WHEN** the client creates an HTTP transport targeting the server's base URL (e.g., `http://localhost:5000/mcp`)
- **THEN** `McpClient.CreateAsync` returns a connected `McpClient` and the initialization handshake completes over HTTP

### Requirement: Client reads server URL from command-line argument or configuration
The HTTP client SHALL accept the server base URL either as a command-line argument or via configuration so it can target a running or test-started server instance.

#### Scenario: Server URL is provided
- **WHEN** the client is invoked with `--server-url http://localhost:5123`
- **THEN** the client connects to `http://localhost:5123`

#### Scenario: Server URL defaults
- **WHEN** the client is invoked without a `--server-url` argument
- **THEN** the client uses a default URL (e.g., `http://localhost:5000`)

### Requirement: Client runs both capability profiles over HTTP
The HTTP client SHALL run both the full-capability profile (Sampling, Roots, Elicitation) and the minimal-capability profile (no capabilities) in a single execution, exactly like the stdio client.

#### Scenario: Full capability profile
- **WHEN** the HTTP client connects with `ClientCapabilities` containing `Sampling`, `Roots`, and `Elicitation`
- **THEN** all 6 primitives (2 tools, 2 prompts, 2 resources) are returned from the server

#### Scenario: Minimal capability profile
- **WHEN** the HTTP client connects with empty `ClientCapabilities`
- **THEN** only the 3 ungated primitives (echo, greeting, app_info) are returned from the server

### Requirement: Client project is a console application
The HTTP client project SHALL be a `Microsoft.NET.Sdk` console application (not a web project) that runs, prints results to stdout, and exits.

#### Scenario: Client exits cleanly
- **WHEN** the client completes both capability profiles
- **THEN** the process exits with code 0 after printing results

### Requirement: Client project has a README
The HTTP client project SHALL contain a `README.md` documenting how to build, configure, and run the client, including how to start the server first for HTTP mode.
