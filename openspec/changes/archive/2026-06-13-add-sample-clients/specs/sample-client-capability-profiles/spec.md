## ADDED Requirements

### Requirement: Full client sees all primitives
A client with `ClientCapabilities` containing `Sampling`, `Roots`, and `Elicitation` SHALL receive all 6 primitives (2 tools, 2 prompts, 2 resources) from the sample server.

#### Scenario: Full client lists tools
- **WHEN** the full-capability client calls `ListToolsAsync()`
- **THEN** the result includes both `ai_summarize` and `echo`

#### Scenario: Full client lists prompts
- **WHEN** the full-capability client calls `ListPromptsAsync()`
- **THEN** the result includes both `confirm_action` and `greeting`

#### Scenario: Full client lists resources
- **WHEN** the full-capability client calls `ListResourcesAsync()`
- **THEN** the result includes both `workspace_files` and `app_info`

### Requirement: Minimal client sees only ungated primitives
A client with empty `ClientCapabilities` (no capabilities set) SHALL receive only the ungated primitives (1 tool, 1 prompt, 1 resource) from the sample server.

#### Scenario: Minimal client lists tools
- **WHEN** the minimal-capability client calls `ListToolsAsync()`
- **THEN** the result includes only `echo` and does NOT include `ai_summarize`

#### Scenario: Minimal client lists prompts
- **WHEN** the minimal-capability client calls `ListPromptsAsync()`
- **THEN** the result includes only `greeting` and does NOT include `confirm_action`

#### Scenario: Minimal client lists resources
- **WHEN** the minimal-capability client calls `ListResourcesAsync()`
- **THEN** the result includes only `app_info` and does NOT include `workspace_files`

### Requirement: Multiple profiles run in a single execution
The sample client SHALL run both capability profiles (full and minimal) in sequence within a single `dotnet run` invocation, printing results for each.

#### Scenario: Both profiles execute
- **WHEN** the client program is run
- **THEN** the full-client results are printed first, followed by the minimal-client results
