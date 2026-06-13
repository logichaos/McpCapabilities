## ADDED Requirements

### Requirement: AddCapabilityGating wires all filtering handlers

The system SHALL provide a builder extension method `AddCapabilityGating(this IMcpServerBuilder builder)` that: preserves any existing `ListToolsHandler`, `ListPromptsHandler`, and `ListResourcesHandler` from `McpServerOptions.Handlers`, wraps them with `CapabilityFilteringHandlers.WrapListTools/WrapListPrompts/WrapListResources`, and sets the wrapped handlers back on `McpServerOptions.Handlers`.

#### Scenario: Existing ListToolsHandler is wrapped and preserved

- **WHEN** `AddCapabilityGating()` is called on a builder that has a custom `ListToolsHandler` set
- **THEN** the resulting `McpServerOptions.Handlers.ListToolsHandler` SHALL be a wrapped handler that calls the original handler and filters its results

#### Scenario: Null handler defaults to empty list

- **WHEN** `AddCapabilityGating()` is called on a builder where `ListToolsHandler` is null
- **THEN** the wrapped handler SHALL return an empty result (no crash)

#### Scenario: Builder is returned for fluent chaining

- **WHEN** `AddCapabilityGating()` is called
- **THEN** the method SHALL return the same `IMcpServerBuilder` instance for further chaining

#### Scenario: All three handlers are wrapped atomically

- **WHEN** `AddCapabilityGating()` is called
- **THEN** `ListToolsHandler`, `ListPromptsHandler`, and `ListResourcesHandler` SHALL all be wrapped with their respective `CapabilityFilteringHandlers.Wrap*` methods

#### Scenario: Does not interfere with non-list handlers

- **WHEN** `AddCapabilityGating()` is called
- **THEN** `CallToolHandler`, `GetPromptHandler`, `ReadResourceHandler`, and other non-list handlers SHALL remain unchanged
