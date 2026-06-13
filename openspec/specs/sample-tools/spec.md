## ADDED Requirements

### Requirement: Gated tool requires Sampling capability
The sample server SHALL expose a tool that requires `CapabilityFlag.Sampling` via `[RequiredClientCapabilities]`. When listed, clients without the `Sampling` capability SHALL NOT see this tool.

#### Scenario: Client with Sampling sees the tool
- **WHEN** a client connects with `ClientCapabilities.Sampling` present
- **THEN** the `tools/list` response includes the `ai_summarize` tool

#### Scenario: Client without Sampling does not see the tool
- **WHEN** a client connects without `ClientCapabilities.Sampling`
- **THEN** the `tools/list` response does NOT include the `ai_summarize` tool

### Requirement: Un-gated tool is always visible
The sample server SHALL expose at least one tool with no `[RequiredClientCapabilities]` attribute. This tool SHALL appear in `tools/list` for every connected client regardless of capabilities.

#### Scenario: Un-gated tool visible to any client
- **WHEN** any client requests `tools/list`
- **THEN** the response includes the un-gated tool (e.g., `echo`)

### Requirement: Tool class is annotated with McpServerToolType
The sample tool class SHALL be marked with `[McpServerToolType]` and contain at least one method annotated with `[McpServerTool]`.
