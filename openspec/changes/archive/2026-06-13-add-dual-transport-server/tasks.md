## 1. Project configuration

- [x] 1.1 Change `SampleMcpServer.csproj` SDK from `Microsoft.NET.Sdk` to `Microsoft.NET.Sdk.Web`
- [x] 1.2 Create `samples/SampleMcpServer/appsettings.json` with `{ "MCP": { "Transport": "stdio" } }`

## 2. Transport switching logic

- [x] 2.1 Replace `Host.CreateApplicationBuilder` with `WebApplication.CreateBuilder` in Program.cs
- [x] 2.2 Read `MCP:Transport` from configuration; default to `"stdio"` if missing or invalid
- [x] 2.3 Conditionally call `WithHttpTransport()` when mode is `"http"` or `"both"`
- [x] 2.4 Conditionally call `app.MapMcp()` when mode is `"http"` or `"both"`
- [x] 2.5 For `"both"` mode, launch stdio via `Task.Run(() => server.RunAsync())` before `app.Run()`
- [x] 2.6 For `"stdio"`-only mode, call `await server.RunAsync()` directly
- [x] 2.7 Remove unused `WithStdioServerTransport()` call

## 3. Documentation

- [x] 3.1 Update `samples/SampleMcpServer/README.md` to document the three transport modes and how to switch via appsettings.json

## 4. Verification

- [x] 4.1 Build the project — confirm zero warnings and zero errors with `Sdk.Web`
- [x] 4.2 Run in `stdio` mode (default) and verify it starts correctly
- [x] 4.3 Run in `http` mode, verify the server starts and listens on HTTP port
- [x] 4.4 Run in `both` mode, verify both transports start without errors
- [x] 4.5 Run the sample client against all three modes and confirm capability filtering still works
