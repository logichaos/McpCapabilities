## ADDED Requirements

### Requirement: Gated prompt requires Elicitation capability
The sample server SHALL expose a prompt that requires `CapabilityFlag.Elicitation` via `[RequiredClientCapabilities]`. When listed, clients without the `Elicitation` capability SHALL NOT see this prompt.

#### Scenario: Client with Elicitation sees the prompt
- **WHEN** a client connects with `ClientCapabilities.Elicitation` present
- **THEN** the `prompts/list` response includes the gated prompt (e.g., `confirm_action`)

#### Scenario: Client without Elicitation does not see the prompt
- **WHEN** a client connects without `ClientCapabilities.Elicitation`
- **THEN** the `prompts/list` response does NOT include the gated prompt

### Requirement: Un-gated prompt is always visible
The sample server SHALL expose at least one prompt with no `[RequiredClientCapabilities]` attribute. This prompt SHALL appear in `prompts/list` for every connected client.

#### Scenario: Un-gated prompt visible to any client
- **WHEN** any client requests `prompts/list`
- **THEN** the response includes the un-gated prompt (e.g., `greeting`)

### Requirement: Prompt class is annotated with McpServerPromptType
The sample prompt class SHALL be marked with `[McpServerPromptType]` and contain at least one method annotated with `[McpServerPrompt]`.
