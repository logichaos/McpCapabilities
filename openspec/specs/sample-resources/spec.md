## ADDED Requirements

### Requirement: Gated resource requires Roots capability
The sample server SHALL expose a resource that requires `CapabilityFlag.Roots` via `[RequiredClientCapabilities]`. When listed, clients without the `Roots` capability SHALL NOT see this resource.

#### Scenario: Client with Roots sees the resource
- **WHEN** a client connects with `ClientCapabilities.Roots` present
- **THEN** the `resources/list` response includes the gated resource (e.g., `workspace://files`)

#### Scenario: Client without Roots does not see the resource
- **WHEN** a client connects without `ClientCapabilities.Roots`
- **THEN** the `resources/list` response does NOT include the gated resource

### Requirement: Un-gated resource is always visible
The sample server SHALL expose at least one resource with no `[RequiredClientCapabilities]` attribute. This resource SHALL appear in `resources/list` for every connected client.

#### Scenario: Un-gated resource visible to any client
- **WHEN** any client requests `resources/list`
- **THEN** the response includes the un-gated resource (e.g., `app://info`)

### Requirement: Resource class is annotated with McpServerResourceType
The sample resource class SHALL be marked with `[McpServerResourceType]` and contain at least one method annotated with `[McpServerResource]`.
