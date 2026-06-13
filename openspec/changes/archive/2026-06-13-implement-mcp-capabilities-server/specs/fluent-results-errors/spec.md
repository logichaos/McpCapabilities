## ADDED Requirements

### Requirement: CapabilityNotMetError implements FluentResults IError

The system SHALL define a class `CapabilityNotMetError` that implements `FluentResults.IError`, with properties `Required` (`CapabilityFlag`), `Missing` (`CapabilityFlag`), `PrimitiveName` (`string`), and `Message` (inherited from `FluentResults.Error`). The constructor SHALL accept the required flags, missing flags, primitive name, and optional message.

#### Scenario: Error carries structured capability data

- **WHEN** a `CapabilityNotMetError` is created with `Required = Sampling | Elicitation`, `Missing = Elicitation`, `PrimitiveName = "ai_summarize"`, `Message = "Missing elicitation"`
- **THEN** its `Required` property SHALL equal `Sampling | Elicitation`
- **AND** its `Missing` property SHALL equal `Elicitation`
- **AND** its `PrimitiveName` property SHALL equal `"ai_summarize"`
- **AND** its `Message` property SHALL equal `"Missing elicitation"`

#### Scenario: Error includes metadata for structured logging

- **WHEN** a `CapabilityNotMetError` is created with the required parameters
- **THEN** its `Metadata` dictionary SHALL contain keys `"RequiredFlags"`, `"MissingFlags"`, and `"PrimitiveName"` with the corresponding string values

#### Scenario: Error is compatible with FluentResults Result<T>

- **WHEN** a `CapabilityNotMetError` is passed to `Result.Fail<T>(error)`
- **THEN** the resulting `Result<T>.IsFailed` SHALL be `true`
- **AND** `Result<T>.Errors` SHALL contain the `CapabilityNotMetError`

### Requirement: FilterByClientCapabilities returns Result<IList<Tool>> with errors

The system SHALL provide an extension method `FilterByClientCapabilities(this IList<Tool> tools, ClientCapabilities? clientCaps)` that returns a `Result<IList<Tool>>` where:
- `Value` contains only tools whose capability requirements are satisfied
- Hidden tools are recorded as `Success` reasons on the result with informational messages
- If all tools are hidden, the result SHALL be a failure with a `CapabilityNotMetError`

#### Scenario: Mixed visible and hidden tools

- **WHEN** `FilterByClientCapabilities` is called with 3 tools (2 require Sampling, 1 requires Elicitation) and a client that only has Sampling
- **THEN** the `Result.Value` SHALL contain 2 tools (the Sampling-requiring tools)
- **AND** `Result.Reasons` SHALL contain a `Success` reason for the hidden tool
- **AND** `Result.IsSuccess` SHALL be `true`

#### Scenario: All tools hidden returns failure

- **WHEN** `FilterByClientCapabilities` is called where all tools require capabilities the client lacks
- **THEN** `Result.IsFailed` SHALL be `true`
- **AND** `Result.Errors` SHALL contain a `CapabilityNotMetError`

#### Scenario: Empty tool list returns success with empty list

- **WHEN** `FilterByClientCapabilities` is called with an empty tool list
- **THEN** `Result.IsSuccess` SHALL be `true`
- **AND** `Result.Value` SHALL be an empty list

### Requirement: FilterByClientCapabilities for prompts and resources

The system SHALL provide equivalent `FilterByClientCapabilities` extension methods for `IList<Prompt>` (returning `Result<IList<Prompt>>`) and `IList<Resource>` (returning `Result<IList<Resource>>`).

#### Scenario: Prompt filtering works identically to tool filtering

- **WHEN** `FilterByClientCapabilities` is called on prompts
- **THEN** the filtering behavior SHALL mirror the tool version using `ClientCapabilityRequirements.ReadFromMeta` on `ProtocolPrompt.Meta`
