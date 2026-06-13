## Context

The `samples/SampleMcpServer` project supports three transport modes (stdio, http, both) controlled by the `MCP:Transport` key in `appsettings.json`. Currently, switching modes requires editing that file manually. The ASP.NET configuration system supports environment-specific `appsettings.{Environment}.json` files and `launchSettings.json` profiles, but these are not yet configured in this project.

## Goals / Non-Goals

**Goals:**
- Add `Properties/launchSettings.json` with three named profiles: `stdio`, `http`, `both`
- Each profile sets `ASPNETCORE_ENVIRONMENT` to `Stdio`, `Http`, or `Both`
- Add environment-specific settings files (`appsettings.Stdio.json`, `appsettings.Http.json`, `appsettings.Both.json`) that set `MCP:Transport` appropriately
- Zero code changes — the existing `Program.cs` already reads `MCP:Transport` from configuration

**Non-Goals:**
- Changing the default transport behavior or the existing `appsettings.json`
- Adding profiles for other projects (SampleMcpClient, SampleMcpHttpClient)
- Modifying the MCP server startup logic

## Decisions

**Decision 1: Environment names match transport values**
- `Stdio`, `Http`, `Both` — PascalCase for .NET convention, matching the transport string values conceptually. Chosen over sentence-case alternatives for consistency with ASP.NET environment naming patterns.
- Each `appsettings.{Environment}.json` file contains only the `MCP:Transport` override; the base `appsettings.json` retains the default `"stdio"` value.

**Decision 2: Keep profile names lowercase**
- Profile names `stdio`, `http`, `both` are lowercase, matching CLI convention (`dotnet run --launch-profile http`). These differ from the environment name for clarity of purpose: profiles are user-facing identifiers, environments are framework configuration keys.

**Decision 3: `commandName` set to `Project`**
- All profiles use `"commandName": "Project"` since the server runs via `dotnet run`. No need for `Executable` or `IISExpress` modes.

**Decision 4: HTTP profiles include applicationUrl**
- The `http` and `both` profiles include `"applicationUrl": "http://localhost:5000"` so the MCP HTTP endpoint is consistently reachable. The `stdio` profile omits this to avoid port binding.

## Risks / Trade-offs

- **Risk:** Adding `launchSettings.json` to `.gitignore` is common but would defeat the purpose here. **Mitigation:** Commit the file so all developers get the profiles.
- **Risk:** Environment names could collide with future ASPNETCORE_ENVIRONMENT values (e.g., `Development`, `Production`). **Mitigation:** `Stdio`, `Http`, `Both` are non-standard and won't collide.
