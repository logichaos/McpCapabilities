## ADDED Requirements

### Requirement: Transport mode read from appsettings.json
The server SHALL read its transport mode from the `MCP:Transport` key in `appsettings.json` at startup. Valid values are `"stdio"`, `"http"`, and `"both"`.

#### Scenario: Default transport is stdio
- **WHEN** no `appsettings.json` exists or the `MCP:Transport` key is absent
- **THEN** the server starts in stdio-only mode

#### Scenario: Configured for HTTP transport
- **WHEN** `MCP:Transport` is set to `"http"` in `appsettings.json`
- **THEN** the server starts with HTTP transport only and does not listen on stdio

#### Scenario: Configured for both transports
- **WHEN** `MCP:Transport` is set to `"both"` in `appsettings.json`
- **THEN** the server starts with both HTTP and stdio transports active

#### Scenario: Invalid transport value falls back to stdio
- **WHEN** `MCP:Transport` is set to an unrecognized value
- **THEN** the server starts in stdio-only mode

### Requirement: Configuration file exists and is documented
The sample project SHALL contain an `appsettings.json` file with the `MCP:Transport` key set to `"stdio"` by default, and the README SHALL document all three valid values.
