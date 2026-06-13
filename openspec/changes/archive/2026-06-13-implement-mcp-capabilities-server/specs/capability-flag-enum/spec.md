## ADDED Requirements

### Requirement: CapabilityFlag enum maps all ClientCapabilities features

The system SHALL define a `[Flags]` enum `CapabilityFlag` in namespace `McpCapabilities.Server` with bitmask values for every feature advertised in the MCP `ClientCapabilities` object: `Sampling`, `Roots`, `Elicitation`, `ElicitationForm`, `ElicitationUrl`, `Tasks`, `TaskList`, `TaskCancel`, `TaskAugmentedSampling`, `TaskAugmentedElicitation`, and a `None = 0` sentinel value.

#### Scenario: Enum contains all client-side MCP capabilities

- **WHEN** `CapabilityFlag` is inspected via reflection
- **THEN** it SHALL include values for Sampling, Roots, Elicitation, ElicitationForm, ElicitationUrl, Tasks, TaskList, TaskCancel, TaskAugmentedSampling, and TaskAugmentedElicitation

#### Scenario: Enum is a Flags enum with None = 0

- **WHEN** `CapabilityFlag.None` is evaluated
- **THEN** its integer value SHALL be 0
- **AND** the enum SHALL have the `[Flags]` attribute

### Requirement: Convert ClientCapabilities to CapabilityFlag bitmask

The system SHALL provide a static method `FromClientCapabilities(ClientCapabilities?)` that converts a `ClientCapabilities` object into a `CapabilityFlag` bitmask by checking each nullable sub-property (e.g., `Sampling`, `Roots`, `Elicitation`, `Tasks`) and their nested properties.

#### Scenario: Client with Sampling capability only

- **WHEN** `FromClientCapabilities` is called with a `ClientCapabilities` where `Sampling` is not null and all other properties are null
- **THEN** the returned `CapabilityFlag` SHALL equal `CapabilityFlag.Sampling`

#### Scenario: Client with Elicitation and ElicitationForm sub-capabilities

- **WHEN** `FromClientCapabilities` is called with a `ClientCapabilities` where `Elicitation` is not null, `Elicitation.Form` is not null, and `Elicitation.Url` is null
- **THEN** the returned `CapabilityFlag` SHALL include both `CapabilityFlag.Elicitation` and `CapabilityFlag.ElicitationForm`
- **AND** SHALL NOT include `CapabilityFlag.ElicitationUrl`

#### Scenario: Client with Tasks and full task sub-capabilities

- **WHEN** `FromClientCapabilities` is called with a `ClientCapabilities` where `Tasks` is not null, `Tasks.List` is not null, `Tasks.Cancel` is not null, `Tasks.Requests.Sampling.CreateMessage` is not null, and `Tasks.Requests.Elicitation.Create` is not null
- **THEN** the returned `CapabilityFlag` SHALL include Tasks, TaskList, TaskCancel, TaskAugmentedSampling, and TaskAugmentedElicitation

#### Scenario: Null ClientCapabilities

- **WHEN** `FromClientCapabilities` is called with a null argument
- **THEN** the method SHALL return `CapabilityFlag.None`

### Requirement: CapabilityFlag bitmask satisfies check

The system SHALL provide a static method `IsSatisfied(CapabilityFlag required, CapabilityFlag available)` that returns `true` if and only if every flag set in `required` is also set in `available`, using the bitmask idiom `(available & required) == required`.

#### Scenario: All required flags present

- **WHEN** `IsSatisfied` is called with `required = Sampling | Elicitation` and `available = Sampling | Elicitation | Roots`
- **THEN** the method SHALL return `true`

#### Scenario: Partial flags missing

- **WHEN** `IsSatisfied` is called with `required = Sampling | Elicitation` and `available = Sampling`
- **THEN** the method SHALL return `false`

#### Scenario: None required

- **WHEN** `IsSatisfied` is called with `required = None` and any `available` value
- **THEN** the method SHALL return `true`
