## ADDED Requirements

### Requirement: Server uses WebApplication builder
The server SHALL use `WebApplication.CreateBuilder` instead of `Host.CreateApplicationBuilder` to enable HTTP transport support.

#### Scenario: WebApplication builder is used
- **WHEN** the server starts
- **THEN** `WebApplication.CreateBuilder(args)` is used to create the host

### Requirement: Server supports WithHttpTransport
When configured for HTTP mode, the server SHALL call `WithHttpTransport()` on the MCP builder during service registration.

#### Scenario: HTTP transport is registered
- **WHEN** transport mode is `"http"` or `"both"`
- **THEN** `WithHttpTransport()` is called on the `IMcpServerBuilder`

### Requirement: Server maps MCP endpoint
When configured for HTTP mode, the server SHALL call `app.MapMcp()` before `app.Run()`.

#### Scenario: MCP endpoint is mapped
- **WHEN** transport mode is `"http"` or `"both"`
- **THEN** `app.MapMcp()` is called to mount the MCP endpoint

### Requirement: Project targets Sdk.Web
The project file SHALL use `Microsoft.NET.Sdk.Web` to provide the ASP.NET Core hosting infrastructure required for HTTP transport.
