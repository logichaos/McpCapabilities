## MODIFIED Requirements

### Requirement: Client connects to sample server via HTTP transport
The web app SHALL use `HttpClientTransport` to connect to `SampleMcpServer` at a configurable URL.

#### Scenario: Client connects successfully
- **WHEN** the web app creates an `McpClient` using `HttpClientTransport` configured for the sample server
- **THEN** an `McpClient` instance is returned and the server is reached

#### Scenario: Client connection fails gracefully
- **WHEN** the MCP server is not running or unreachable
- **THEN** the web app displays an error page or message without crashing

### Requirement: Client lists and displays all primitive types as HTML
The web app SHALL call `ListToolsAsync()`, `ListPromptsAsync()`, and `ListResourcesAsync()` on the connected `McpClient` and render the results as HTML elements on Razor Pages.

#### Scenario: Client lists tools as HTML
- **WHEN** a profile page calls `ListToolsAsync()`
- **THEN** the returned tool names are displayed as an HTML list on the page

#### Scenario: Client lists prompts as HTML
- **WHEN** a profile page calls `ListPromptsAsync()`
- **THEN** the returned prompt names are displayed as an HTML list on the page

#### Scenario: Client lists resources as HTML
- **WHEN** a profile page calls `ListResourcesAsync()`
- **THEN** the returned resource names are displayed as an HTML list on the page

### Requirement: Client project README documents web app build and run
The web app project SHALL contain a `README.md` with instructions for building (`dotnet build`), running the web app (`dotnet run`), starting the required MCP server in HTTP mode, and the expected URL to open in a browser.

#### Scenario: README describes web app usage
- **WHEN** the README is opened
- **THEN** it contains build instructions, run instructions for both server and web app, the default browser URL, and descriptions of each page
