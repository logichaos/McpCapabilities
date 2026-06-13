## Why

The SampleMcpServer currently requires manually editing `appsettings.json` to switch between stdio and HTTP transport modes before running. This is error-prone and slow during development. Adding `launchSettings.json` with distinct profiles and corresponding `appsettings.{profile}.json` files lets developers choose the transport mode at launch time without modifying the default configuration.

## What Changes

- Add `Properties/launchSettings.json` to the SampleMcpServer project with three profiles: `stdio`, `http`, and `both`
- Add `appsettings.Stdio.json`, `appsettings.Http.json`, and `appsettings.Both.json` files, each setting `MCP:Transport` to the appropriate value
- Each `launchSettings.json` profile sets `ASPNETCORE_ENVIRONMENT` to the matching name so ASP.NET loads the correct environment-specific settings file

## Capabilities

### New Capabilities

- `sample-launch-profiles`: Launch profiles and environment-specific configuration for choosing stdio, http, or both transport modes at startup without editing the default `appsettings.json`

### Modified Capabilities

None — existing spec requirements for dual-transport-configuration, http-transport, both-transport-mode, and sample-hosting remain unchanged. This change only adds developer launch-time configuration on top of existing behavior.

## Impact

- Affected project: `samples/SampleMcpServer/`
- New files: `Properties/launchSettings.json`, `appsettings.Stdio.json`, `appsettings.Http.json`, `appsettings.Both.json`
- No code changes, no API changes, no breaking changes
