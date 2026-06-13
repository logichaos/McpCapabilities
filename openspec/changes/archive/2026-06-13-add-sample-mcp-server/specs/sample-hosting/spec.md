## ADDED Requirements

### Requirement: Server uses stdio transport
The sample server SHALL use `WithStdioServerTransport()` to accept MCP connections over standard input/output.

#### Scenario: Server starts and listens on stdio
- **WHEN** the server process starts
- **THEN** the server is ready to accept MCP requests over stdin/stdout

### Requirement: Server registers capability-aware tools
The sample server SHALL call `WithCapabilityAwareTools<AiTools>()` to register tools and capture `[RequiredClientCapabilities]` attributes at startup.

#### Scenario: Tool capabilities are captured during startup
- **WHEN** the server starts with `WithCapabilityAwareTools<AiTools>()` in the builder
- **THEN** the `ProtocolTool.Meta` of each annotated tool is populated with capability requirements

### Requirement: Server registers capability-aware prompts
The sample server SHALL call a mechanism equivalent to `WithCapabilityAwareTools` for prompts, or manually call `CaptureCapabilityRequirements()` on each registered prompt, so that prompt capability requirements are captured at startup.

#### Scenario: Prompt capabilities are captured during startup
- **WHEN** the server starts and registers `HelpfulPrompts` with capability capture
- **THEN** the `ProtocolPrompt.Meta` of each annotated prompt is populated with capability requirements

### Requirement: Server registers capability-aware resources
The sample server SHALL call a mechanism equivalent to `WithCapabilityAwareTools` for resources, or manually call `CaptureCapabilityRequirements()` on each registered resource, so that resource capability requirements are captured at startup.

#### Scenario: Resource capabilities are captured during startup
- **WHEN** the server starts and registers `WorkspaceResources` with capability capture
- **THEN** the `ProtocolResource.Meta` of each annotated resource is populated with capability requirements

### Requirement: Server enables capability gating
The sample server SHALL call `AddCapabilityGating()` so that list handlers wrap the full lists with capability-based filtering at request time.

#### Scenario: Filtering is active after AddCapabilityGating
- **WHEN** the server starts with `AddCapabilityGating()` in the builder
- **THEN** the `ListToolsHandler`, `ListPromptsHandler`, and `ListResourcesHandler` are wrapped with capability filters

### Requirement: Sample README documents how to build and run
The sample directory SHALL contain a `README.md` with instructions for building (`dotnet build`) and running (`dotnet run`), along with a brief description of what the sample demonstrates.
