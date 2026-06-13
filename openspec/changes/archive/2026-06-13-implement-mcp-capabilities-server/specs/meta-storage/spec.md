## ADDED Requirements

### Requirement: ClientCapabilityRequirements record stores requirements

The system SHALL define a `readonly record struct` named `ClientCapabilityRequirements` in namespace `McpCapabilities.Server` with properties `Required` (`CapabilityFlag`) and `Message` (`string?`), and a static `None` singleton representing no requirements.

#### Scenario: None singleton has no requirements

- **WHEN** `ClientCapabilityRequirements.None` is accessed
- **THEN** its `Required` property SHALL equal `CapabilityFlag.None`
- **AND** its `Message` property SHALL be null

#### Scenario: Instance with Sampling requirement

- **WHEN** a `ClientCapabilityRequirements` is created with `Required = Sampling` and `Message = "Needs LLM"`
- **THEN** `Required` SHALL equal `CapabilityFlag.Sampling`
- **AND** `Message` SHALL equal `"Needs LLM"`

### Requirement: Write requirements to ProtocolTool.Meta

The system SHALL provide a method `WriteToMeta(JsonObject meta)` on `ClientCapabilityRequirements` that writes the requirements into the given `JsonObject` under the well-known key `__mcp_capabilities_required`, storing `flags` as the string representation of the `CapabilityFlag` and `message` as the optional message string.

#### Scenario: Write requirements with both flags and message

- **WHEN** `WriteToMeta` is called with `Required = Sampling | Elicitation` and `Message = "Needs LLM and confirmation"`
- **THEN** the JsonObject SHALL contain key `__mcp_capabilities_required`
- **AND** its `flags` field SHALL equal `"Sampling, Elicitation"`
- **AND** its `message` field SHALL equal `"Needs LLM and confirmation"`

#### Scenario: Write requirements with no message

- **WHEN** `WriteToMeta` is called with `Required = Sampling` and `Message = null`
- **THEN** the `message` field in the JSON SHALL be a null JSON value

### Requirement: Read requirements from ProtocolTool.Meta

The system SHALL provide a static method `ReadFromMeta(JsonObject? meta)` on `ClientCapabilityRequirements` that reads the requirements from the given JsonObject, returning `None` if the meta is null or the key `__mcp_capabilities_required` is absent.

#### Scenario: Read requirements from populated meta

- **WHEN** `ReadFromMeta` is called with a JsonObject containing `__mcp_capabilities_required` with `flags = "Sampling, Elicitation"` and `message = "test"`
- **THEN** the returned value SHALL have `Required = Sampling | Elicitation` and `Message = "test"`

#### Scenario: Read requirements from null meta

- **WHEN** `ReadFromMeta` is called with a null argument
- **THEN** the returned value SHALL equal `ClientCapabilityRequirements.None`

#### Scenario: Read requirements from meta without the key

- **WHEN** `ReadFromMeta` is called with an empty JsonObject
- **THEN** the returned value SHALL equal `ClientCapabilityRequirements.None`

### Requirement: IsSatisfiedBy checks ClientCapabilities

The system SHALL provide a method `IsSatisfiedBy(ClientCapabilities?)` on `ClientCapabilityRequirements` that returns `true` when all required capability flags are present in the given `ClientCapabilities`, and `false` otherwise.

#### Scenario: Requirements satisfied by full capabilities

- **WHEN** `IsSatisfiedBy` is called on a `ClientCapabilityRequirements` with `Required = Sampling` against a `ClientCapabilities` where `Sampling` is not null
- **THEN** the method SHALL return `true`

#### Scenario: Requirements not satisfied by partial capabilities

- **WHEN** `IsSatisfiedBy` is called on a `ClientCapabilityRequirements` with `Required = Sampling | Elicitation` against a `ClientCapabilities` where only `Sampling` is not null
- **THEN** the method SHALL return `false`

#### Scenario: Requirements satisfied when none required

- **WHEN** `IsSatisfiedBy` is called on `ClientCapabilityRequirements.None` against any `ClientCapabilities` (including null)
- **THEN** the method SHALL return `true`

#### Scenario: Requirements not satisfied against null capabilities

- **WHEN** `IsSatisfiedBy` is called on a `ClientCapabilityRequirements` with `Required = Sampling` against a null `ClientCapabilities`
- **THEN** the method SHALL return `false`

### Requirement: Extension methods on McpServerTool for capture and read

The system SHALL provide extension methods `CaptureCapabilityRequirements(this McpServerTool)` and `GetCapabilityRequirements(this McpServerTool)` that write to and read from `ProtocolTool.Meta` respectively.

#### Scenario: Capture reads [RequiredClientCapabilities] from MethodInfo

- **WHEN** `CaptureCapabilityRequirements` is called on an `McpServerTool` whose underlying method has `[RequiredClientCapabilities(Required = Sampling)]`
- **THEN** `ProtocolTool.Meta` SHALL contain the `__mcp_capabilities_required` key with `flags = "Sampling"`

#### Scenario: Capture on tool without attribute does nothing

- **WHEN** `CaptureCapabilityRequirements` is called on an `McpServerTool` whose underlying method has no `[RequiredClientCapabilities]`
- **THEN** `ProtocolTool.Meta` SHALL remain unchanged (or remain null)

#### Scenario: GetCapabilityRequirements reads from meta

- **WHEN** `GetCapabilityRequirements` is called after `CaptureCapabilityRequirements` has been invoked with a Sampling requirement
- **THEN** the returned `ClientCapabilityRequirements` SHALL have `Required = Sampling`

### Requirement: Extension methods on McpServerPrompt for capture and read

The system SHALL provide extension methods `CaptureCapabilityRequirements(this McpServerPrompt)` and `GetCapabilityRequirements(this McpServerPrompt)` mirroring the tool equivalents.

#### Scenario: Capture works for prompt

- **WHEN** `CaptureCapabilityRequirements` is called on an `McpServerPrompt` whose underlying method has `[RequiredClientCapabilities(Required = Elicitation)]`
- **THEN** `ProtocolPrompt.Meta` SHALL contain the `__mcp_capabilities_required` key

### Requirement: Extension methods on McpServerResource for capture and read

The system SHALL provide extension methods `CaptureCapabilityRequirements(this McpServerResource)` and `GetCapabilityRequirements(this McpServerResource)` mirroring the tool equivalents.

#### Scenario: Capture works for resource

- **WHEN** `CaptureCapabilityRequirements` is called on an `McpServerResource` whose underlying method has `[RequiredClientCapabilities(Required = Roots)]`
- **THEN** `ProtocolResource.Meta` SHALL contain the `__mcp_capabilities_required` key
