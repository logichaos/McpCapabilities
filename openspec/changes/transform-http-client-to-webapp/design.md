## Context

`SampleMcpHttpClient` is currently a .NET console application that uses `HttpClientTransport` to connect to an MCP server and prints capability-gated results to stdout. The project sits at `samples/SampleMcpHttpClient/` alongside `SampleMcpServer` and `SampleMcpClient`. The goal is to replace the console UX with a Razor Pages web application that provides a browsable, interactive interface.

Current dependencies: `ModelContextProtocol` NuGet package. Target framework: `net10.0`. No existing web framework references.

## Goals / Non-Goals

**Goals:**
- Replace the console entry point with an ASP.NET Core web host
- Provide Razor Pages that display MCP primitives (tools, prompts, resources) as HTML
- Support both Full and Minimal capability profiles navigable via nav menu
- Add a home/dashboard page with connection status and summary
- Keep the existing `HttpClientTransport` for MCP server connectivity
- Update `README.md` with build/run instructions for the web app

**Non-Goals:**
- Adding authentication, authorization, or user sessions
- Real-time updates via WebSocket or SignalR
- Calling MCP tools/prompts (listing only, no invocation)
- Adding a separate API layer — Razor Pages talk directly to the MCP client
- Changing the `SampleMcpServer` or its behavior
- Adding CSS/JS frameworks — use plain Razor with minimal styling

## Decisions

### Decision 1: Razor Pages over MVC

**Choice:** Razor Pages

**Rationale:** The app has simple page-per-view navigation (Dashboard, Full Profile, Minimal Profile) with no complex form workflows. Razor Pages align naturally with this page-centric model and produce fewer files than MVC. Each profile page is a self-contained PageModel + .cshtml pair.

**Alternatives considered:**
- *MVC*: Overkill for 3-4 simple pages. Requires separate controller, view, and model for each page.
- *Blazor Server*: Adds SignalR dependency and persistent circuit overhead for a read-only listing app.
- *Minimal API + static HTML*: No server-side rendering; would require JavaScript to fetch and render data, adding complexity.

### Decision 2: MCP client lifetime — scoped per request

**Choice:** Create a new `IMcpClient` per page request using `IHttpClientFactory` for the underlying HTTP connection.

**Rationale:** Razor Pages are request-scoped. The MCP client connects, lists primitives, and disconnects within a single request. This avoids lifetime mismatches between singleton clients and scoped pages.

**Alternatives considered:**
- *Singleton MCP client*: Connection state management becomes complex. A disconnected client requires reconnect logic that adds more complexity than per-request creation.
- *Background service with cached results*: Overkill. Adds memory overhead for data that may be stale by the time it's viewed.

### Decision 3: Inline styling — no CSS framework

**Choice:** Use minimal inline styles or a small `<style>` block in `_Layout.cshtml`. No external CSS framework.

**Rationale:** The app is a sample demonstrating MCP concepts, not a polished product. Adding Bootstrap or Tailwind adds visual noise and distracts from the MCP data being displayed.

### Decision 4: Navigation via `_Layout.cshtml` with nav links

**Choice:** A shared layout with a `<nav>` containing links to Dashboard, Full Profile, and Minimal Profile pages.

**Rationale:** Standard Razor Pages pattern. The layout provides consistent chrome without a partial or ViewComponent.

## Risks / Trade-offs

- **Per-request MCP client creation** → Each page load incurs a full MCP handshake (initialize + list). Acceptable for a sample app with low traffic. Mitigation: document expected latency in README.
- **Server dependency** → The web app cannot display anything if `SampleMcpServer` is not running in HTTP mode. Mitigation: Dashboard page clearly shows connection status (connected/error) with actionable message.
- **No JavaScript interactivity** → Lists are static HTML. Users must reload to refresh data. Acceptable trade-off for simplicity.
