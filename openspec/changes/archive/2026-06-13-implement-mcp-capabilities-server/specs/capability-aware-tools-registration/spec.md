## ADDED Requirements

### Requirement: WithCapabilityAwareTools<T> registers tools and captures attributes

The system SHALL provide a builder extension method `WithCapabilityAwareTools<TToolType>(this IMcpServerBuilder builder)` and its overload `WithCapabilityAwareTools<TToolType>(this IMcpServerBuilder builder, Action<McpServerTool, ClientCapabilityRequirements>? configure)` that: registers tools from type `TToolType` using the standard SDK pipeline, then post-processes each `McpServerTool` to read `[RequiredClientCapabilities]` attributes via reflection and store them in `ProtocolTool.Meta`.

#### Scenario: Tool with attribute gets captured

- **WHEN** `WithCapabilityAwareTools<MyTools>()` is called where `MyTools` has a method annotated with `[RequiredClientCapabilities(Required = Sampling)]`
- **THEN** after registration, the tool's `ProtocolTool.Meta` SHALL contain `__mcp_capabilities_required` with `flags = "Sampling"`

#### Scenario: Tool without attribute is not modified

- **WHEN** `WithCapabilityAwareTools<MyTools>()` is called where `MyTools` has a method with no `[RequiredClientCapabilities]`
- **THEN** after registration, the tool's `ProtocolTool.Meta` SHALL NOT contain `__mcp_capabilities_required`

#### Scenario: Configure callback is invoked per tool

- **WHEN** `WithCapabilityAwareTools<MyTools>(configure: (tool, reqs) => { ... })` is called
- **THEN** the configure callback SHALL be invoked for each tool that has a `[RequiredClientCapabilities]` attribute, with the tool and its captured requirements

#### Scenario: Reflection happens once at registration time

- **WHEN** `WithCapabilityAwareTools<T>()` has completed
- **THEN** no further reflection SHALL occur during subsequent `tools/list` requests
