## ADDED Requirements

### Requirement: Web app hosts an ASP.NET Core Razor Pages application
The `SampleMcpHttpClient` project SHALL be an ASP.NET Core Razor Pages web application that starts an HTTP server and serves HTML pages.

#### Scenario: Web app starts and serves pages
- **WHEN** the web app is launched with `dotnet run`
- **THEN** it starts an HTTP server on a configurable port and serves Razor Pages

#### Scenario: Web app is reachable in a browser
- **WHEN** a browser navigates to the web app's root URL
- **THEN** the Dashboard page is displayed with a 200 OK response

### Requirement: Dashboard page shows connection status and summary
The web app SHALL have a Dashboard page (`/Index`) that displays whether the MCP server is reachable and a summary of available primitives.

#### Scenario: Dashboard shows connected status
- **WHEN** the MCP server is running and reachable at the configured endpoint
- **THEN** the Dashboard displays "Connected" with the server URL and aggregate counts of tools, prompts, and resources

#### Scenario: Dashboard shows disconnected status
- **WHEN** the MCP server is not running or unreachable
- **THEN** the Dashboard displays an error message indicating the server is not available, along with instructions to start it

### Requirement: Full Profile page displays all primitives
The web app SHALL have a Full Profile page (`/FullProfile`) that connects with `ClientCapabilities` containing `Sampling`, `Roots`, and `Elicitation` and displays all gated primitives as HTML.

#### Scenario: Full Profile lists tools
- **WHEN** the Full Profile page loads
- **THEN** it displays a list of available tools under a "Tools" heading, including both `ai_summarize` and `echo`

#### Scenario: Full Profile lists prompts
- **WHEN** the Full Profile page loads
- **THEN** it displays a list of available prompts under a "Prompts" heading, including both `confirm_action` and `greeting`

#### Scenario: Full Profile lists resources
- **WHEN** the Full Profile page loads
- **THEN** it displays a list of available resources under a "Resources" heading, including both `workspace_files` and `app_info`

### Requirement: Minimal Profile page displays only ungated primitives
The web app SHALL have a Minimal Profile page (`/MinimalProfile`) that connects with empty `ClientCapabilities` and displays only ungated primitives as HTML.

#### Scenario: Minimal Profile lists only ungated tools
- **WHEN** the Minimal Profile page loads
- **THEN** it displays `echo` under "Tools" but does NOT include `ai_summarize`

#### Scenario: Minimal Profile lists only ungated prompts
- **WHEN** the Minimal Profile page loads
- **THEN** it displays `greeting` under "Prompts" but does NOT include `confirm_action`

#### Scenario: Minimal Profile lists only ungated resources
- **WHEN** the Minimal Profile page loads
- **THEN** it displays `app_info` under "Resources" but does NOT include `workspace_files`

### Requirement: Navigation is available on all pages
The web app SHALL provide navigation links to Dashboard, Full Profile, and Minimal Profile pages on every page via a shared layout.

#### Scenario: Nav links are present on Dashboard
- **WHEN** the Dashboard page is displayed
- **THEN** navigation links to "Full Profile" and "Minimal Profile" are visible

#### Scenario: Nav links are present on profile pages
- **WHEN** any profile page is displayed
- **THEN** navigation links to "Dashboard" and the other profile are visible

### Requirement: README documents web app usage
The web app project SHALL contain a `README.md` with instructions for building (`dotnet build`), running (`dotnet run`), and starting the required MCP server in HTTP mode.

#### Scenario: README exists with build and run instructions
- **WHEN** the README is opened
- **THEN** it contains `dotnet build` and `dotnet run` commands, server startup instructions, and expected output descriptions
