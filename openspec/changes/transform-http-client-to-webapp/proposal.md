## Why

The current `SampleMcpHttpClient` is a console application that dumps MCP primitive lists to stdout. This provides no visual interface, no interactivity, and no way to explore the MCP server's capabilities beyond reading terminal output. A web application gives users a browsable, point-and-click experience that better demonstrates the value of capability-gated MCP servers.

## What Changes

- **Replace** the console `SampleMcpHttpClient` with an ASP.NET Core Razor Pages web application
- Add a web UI with pages that connect to the MCP server via `HttpClientTransport` and display capability-gated tools, prompts, and resources
- Add multiple capability profile pages (Full and Minimal) navigable from a sidebar or nav menu
- Add a home/dashboard page showing connection status and summary
- Add a `README.md` with build/run instructions for the web app
- **Remove** the console `dotnet run` output — all results are rendered as HTML pages

## Capabilities

### New Capabilities
- `sample-web-app`: The web application UI that connects to the sample MCP server over HTTP transport, displays capability-gated primitives as navigable HTML pages, and supports multiple capability profiles.

### Modified Capabilities
- `sample-client-hosting`: The hosting model changes from a console `Program.cs` entry point to an ASP.NET Core web application with Razor Pages. The core requirement — that the client connects to the sample server and lists primitives — remains but shifts from stdout to HTML rendering.

## Impact

- `samples/SampleMcpHttpClient/`: Complete rewrite from console app to Razor Pages web app
- `samples/SampleMcpHttpClient/Program.cs`: Replaced with ASP.NET Core web host + MCP client setup
- New Razor Pages added under `samples/SampleMcpHttpClient/Pages/`
- NuGet dependencies change: add ASP.NET Core framework reference, keep `ModelContextProtocol`
- `samples/SampleMcpHttpClient/README.md`: Updated with web app-specific instructions
