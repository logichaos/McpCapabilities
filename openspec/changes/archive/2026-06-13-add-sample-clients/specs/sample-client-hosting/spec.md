## ADDED Requirements

### Requirement: Client connects to sample server via stdio
The client SHALL use `StdioClientTransport` to connect to `SampleMcpServer` by launching `dotnet run --project` pointing to the sample server project directory.

#### Scenario: Client connects successfully
- **WHEN** the client executes `McpClient.CreateAsync` with a `StdioClientTransport` configured for the sample server
- **THEN** an `McpClient` instance is returned and the server is running

### Requirement: Client capabilities are configurable
The client SHALL accept a `ClientCapabilities` configuration via `McpClientOptions` so that different capability profiles can be tested.

#### Scenario: Full capabilities client
- **WHEN** the client is created with `ClientCapabilities` containing `Sampling`, `Roots`, and `Elicitation`
- **THEN** these capabilities are advertised to the server during the initialization handshake

#### Scenario: Minimal capabilities client
- **WHEN** the client is created with an empty `ClientCapabilities` (no capabilities set)
- **THEN** no optional capabilities are advertised to the server

### Requirement: Client lists and prints all primitive types
The client SHALL call `ListToolsAsync()`, `ListPromptsAsync()`, and `ListResourcesAsync()` on the connected `McpClient` and print the results to the console.

#### Scenario: Client lists tools
- **WHEN** the client calls `ListToolsAsync()`
- **THEN** the returned tool names are printed to stdout

#### Scenario: Client lists prompts
- **WHEN** the client calls `ListPromptsAsync()`
- **THEN** the returned prompt names are printed to stdout

#### Scenario: Client lists resources
- **WHEN** the client calls `ListResourcesAsync()`
- **THEN** the returned resource names are printed to stdout

### Requirement: Client project README documents build and run
The client project SHALL contain a `README.md` with instructions for building (`dotnet build`) and running (`dotnet run`), along with a description of what capability profiles are tested and expected results.
