## 1. Environment-specific settings files

- [x] 1.1 Create `appsettings.Stdio.json` with `MCP:Transport` set to `"stdio"`
- [x] 1.2 Create `appsettings.Http.json` with `MCP:Transport` set to `"http"`
- [x] 1.3 Create `appsettings.Both.json` with `MCP:Transport` set to `"both"`

## 2. Launch profiles

- [x] 2.1 Create `Properties/launchSettings.json` with three profiles (`stdio`, `http`, `both`), each using `"commandName": "Project"` and setting the appropriate `ASPNETCORE_ENVIRONMENT`
- [x] 2.2 Set `applicationUrl` to `http://localhost:5000` for the `http` and `both` profiles only

## 3. Verification

- [x] 3.1 Run `dotnet run --launch-profile stdio` and confirm the server starts in stdio mode
- [x] 3.2 Run `dotnet run --launch-profile http` and confirm the server starts in HTTP mode on port 5000
- [x] 3.3 Run `dotnet run --launch-profile both` and confirm both transports start
- [x] 3.4 Run `dotnet run` with no profile and confirm it falls back to the default `appsettings.json` (stdio)
