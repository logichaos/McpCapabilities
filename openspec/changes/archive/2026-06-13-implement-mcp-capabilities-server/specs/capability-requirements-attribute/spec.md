## ADDED Requirements

### Requirement: RequiredClientCapabilities attribute exists and targets methods

The system SHALL define a sealed attribute class `RequiredClientCapabilitiesAttribute` in namespace `McpCapabilities.Server` that can be applied to methods only, is not inherited, and does not allow multiple applications on the same target.

#### Scenario: Attribute applied to a tool method compiles

- **WHEN** a developer annotates a method with `[RequiredClientCapabilities(Required = CapabilityFlag.Sampling)]`
- **THEN** the code SHALL compile successfully

#### Scenario: Attribute cannot be applied to a class

- **WHEN** a developer attempts to annotate a class with `[RequiredClientCapabilities]`
- **THEN** the code SHALL produce a compilation error (AttributeUsage restricted to Method)

#### Scenario: Attribute cannot be inherited

- **WHEN** a derived class overrides a method that has `[RequiredClientCapabilities]`
- **THEN** the overridden method SHALL NOT inherit the attribute

### Requirement: Attribute has Required and Message properties

The system SHALL define the `RequiredClientCapabilitiesAttribute` with an `init`-only property `Required` of type `CapabilityFlag` for the bitmask of required client capabilities, and an optional `init`-only property `Message` of type `string?` for a human-readable description shown when requirements are not met.

#### Scenario: Attribute with only Required property

- **WHEN** `[RequiredClientCapabilities(Required = CapabilityFlag.Sampling | CapabilityFlag.Elicitation)]` is declared
- **THEN** the `Required` property SHALL equal `Sampling | Elicitation`
- **AND** the `Message` property SHALL be null

#### Scenario: Attribute with Required and Message properties

- **WHEN** `[RequiredClientCapabilities(Required = CapabilityFlag.Sampling, Message = "Needs LLM support")]` is declared
- **THEN** the `Required` property SHALL equal `CapabilityFlag.Sampling`
- **AND** the `Message` property SHALL equal `"Needs LLM support"`
