## ADDED Requirements

### Requirement: Single IMcpServer instance serves both transports
When configured for `"both"` mode, the server SHALL use a single `IMcpServer` instance registered in the DI container to serve both stdio and HTTP transports.

#### Scenario: Both transports share the same server instance
- **WHEN** transport mode is `"both"`
- **THEN** the same `IMcpServer` registered via `AddMcpServer()` handles both stdio connections and HTTP requests

### Requirement: Stdio transport runs in background task
When configured for `"both"` mode, the stdio transport SHALL be started in a background `Task.Run` so it does not block the HTTP host.

#### Scenario: Stdio runs concurrently with HTTP
- **WHEN** transport mode is `"both"`
- **THEN** the stdio server runs in a background task while `app.Run()` handles HTTP requests on the main thread

### Requirement: Tool visibility is consistent across transports
Capability-gated primitives SHALL be hidden or shown identically regardless of which transport a client connects through.

#### Scenario: Same filtering applies to both transports
- **WHEN** a minimal-capability client connects via HTTP
- **THEN** the same filtering rules apply as for a minimal-capability client connecting via stdio

### Requirement: Graceful shutdown handles both transports
When the server process receives a shutdown signal, both the HTTP host and the stdio background task SHALL terminate cleanly without unhandled exceptions.

#### Scenario: Ctrl+C stops both transports
- **WHEN** the user presses Ctrl+C while in `"both"` mode
- **THEN** both the HTTP listener and stdio transport shut down without crashing
