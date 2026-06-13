## ADDED Requirements

### Requirement: WrapListTools filters tools by ClientCapabilities

The system SHALL provide a static method `WrapListTools(McpRequestHandler<ListToolsRequestParams, ListToolsResult>? inner)` in class `CapabilityFilteringHandlers` that returns a handler delegate which: calls the inner handler (if provided) to get the full tool list, reads `ClientCapabilityRequirements` from each tool's `Meta`, compares against `ClientCapabilities` from the request's `McpServer`, and returns a filtered list excluding tools whose requirements are not met.

#### Scenario: All tools visible when all requirements met

- **WHEN** a wrapped handler receives a `tools/list` request from a client with `ClientCapabilities.Sampling` not null
- **AND** the full tool list contains one tool with `Required = Sampling` and one tool with no requirements
- **THEN** the filtered result SHALL contain both tools

#### Scenario: Tool hidden when requirement not met

- **WHEN** a wrapped handler receives a `tools/list` request from a client with empty `ClientCapabilities`
- **AND** the full tool list contains one tool with `Required = Sampling`
- **THEN** the filtered result SHALL NOT contain that tool

#### Scenario: Tools without requirements always visible

- **WHEN** a wrapped handler receives a `tools/list` request from a client with null `ClientCapabilities`
- **AND** the full tool list contains one tool with `Meta` that has no `__mcp_capabilities_required` key
- **THEN** the filtered result SHALL contain that tool

#### Scenario: Empty result when all tools filtered out

- **WHEN** a wrapped handler receives a `tools/list` request from a client with empty `ClientCapabilities`
- **AND** all tools have unsatisfied requirements
- **THEN** the filtered result SHALL contain zero tools

#### Scenario: Inner handler called when provided

- **WHEN** a non-null inner handler is passed to `WrapListTools`
- **AND** the inner handler returns 3 tools
- **THEN** the wrapped handler SHALL invoke the inner handler and filter its result

#### Scenario: Default empty list when inner handler is null

- **WHEN** a null inner handler is passed to `WrapListTools`
- **THEN** the wrapped handler SHALL return an empty `ListToolsResult`

### Requirement: WrapListPrompts filters prompts by ClientCapabilities

The system SHALL provide a static method `WrapListPrompts` in `CapabilityFilteringHandlers` that filters prompts in the same manner as `WrapListTools`, using `ProtocolPrompt.Meta` to read `ClientCapabilityRequirements`.

#### Scenario: Prompt hidden when requirement not met

- **WHEN** a wrapped prompt handler receives a `prompts/list` request from a client lacking `Elicitation`
- **AND** the full prompt list contains a prompt with `Required = Elicitation`
- **THEN** the filtered result SHALL NOT contain that prompt

### Requirement: WrapListResources filters resources by ClientCapabilities

The system SHALL provide a static method `WrapListResources` in `CapabilityFilteringHandlers` that filters resources using `ProtocolResource.Meta` in the same manner.

#### Scenario: Resource visible when no requirements

- **WHEN** a wrapped resource handler receives a `resources/list` request
- **AND** a resource has no capability requirements in its `Meta`
- **THEN** that resource SHALL be included in the filtered result

### Requirement: Filtering is zero-reflection at request time

The filtering handler delegates SHALL NOT use reflection. They SHALL read requirements exclusively from the `Protocol*.Meta` JsonObject, which was populated at registration time.

#### Scenario: No MethodInfo or GetCustomAttribute calls during filtering

- **WHEN** a wrapped handler filters tool lists
- **THEN** the code path SHALL NOT reference `MethodInfo`, `GetCustomAttribute`, or any `System.Reflection` type
