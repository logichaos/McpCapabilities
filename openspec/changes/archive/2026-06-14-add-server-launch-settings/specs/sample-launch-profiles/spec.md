## ADDED Requirements

### Requirement: Launch profiles for each transport mode
The SampleMcpServer project SHALL contain a `Properties/launchSettings.json` file with three named profiles: `stdio`, `http`, and `both`, each using `"commandName": "Project"` and setting `ASPNETCORE_ENVIRONMENT` to the corresponding environment name.

#### Scenario: Stdio profile sets Stdio environment
- **WHEN** the developer selects the `stdio` launch profile
- **THEN** `ASPNETCORE_ENVIRONMENT` is set to `Stdio`

#### Scenario: Http profile sets Http environment
- **WHEN** the developer selects the `http` launch profile
- **THEN** `ASPNETCORE_ENVIRONMENT` is set to `Http`

#### Scenario: Both profile sets Both environment
- **WHEN** the developer selects the `both` launch profile
- **THEN** `ASPNETCORE_ENVIRONMENT` is set to `Both`

#### Scenario: Http and both profiles include application URL
- **WHEN** the `http` or `both` launch profile is used
- **THEN** `applicationUrl` is set to `http://localhost:5000`

#### Scenario: Stdio profile does not include application URL
- **WHEN** the `stdio` launch profile is used
- **THEN** no `applicationUrl` is specified so the server does not bind an HTTP port

### Requirement: Environment-specific settings files override transport mode
The SampleMcpServer project SHALL contain `appsettings.Stdio.json`, `appsettings.Http.json`, and `appsettings.Both.json` files, each setting `MCP:Transport` to the corresponding value (`"stdio"`, `"http"`, or `"both"`).

#### Scenario: Stdio environment sets stdio transport
- **WHEN** `ASPNETCORE_ENVIRONMENT` is `Stdio`
- **THEN** the server reads `MCP:Transport` from `appsettings.Stdio.json` and starts in stdio-only mode

#### Scenario: Http environment sets http transport
- **WHEN** `ASPNETCORE_ENVIRONMENT` is `Http`
- **THEN** the server reads `MCP:Transport` from `appsettings.Http.json` and starts in HTTP-only mode

#### Scenario: Both environment sets both transport
- **WHEN** `ASPNETCORE_ENVIRONMENT` is `Both`
- **THEN** the server reads `MCP:Transport` from `appsettings.Both.json` and starts with both transports active

#### Scenario: Default appsettings.json is unchanged
- **WHEN** no environment-specific override applies
- **THEN** the server reads `MCP:Transport` from `appsettings.json` which retains the default value of `"stdio"`
