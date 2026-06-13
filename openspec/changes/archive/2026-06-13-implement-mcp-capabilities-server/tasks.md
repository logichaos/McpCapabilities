## 1. Project Setup

- [x] 1.1 Create `src/McpCapabilities.Server/McpCapabilities.Server.csproj` targeting `net10.0` with `ImplicitUsings`, `Nullable`, package references for `ModelContextProtocol`, `FluentResults`, `Microsoft.Extensions.DependencyInjection`, and `Microsoft.Extensions.Options`
- [x] 1.2 Add `McpCapabilities.Server` project to `McpCapabilities.slnx`
- [x] 1.3 Add `ModelContextProtocol` and `FluentResults` package versions to `Directory.Packages.props`
- [x] 1.4 Add project reference from both test projects to `McpCapabilities.Server`

## 2. CapabilityFlag Enum & Converter (spec: capability-flag-enum)

- [x] 2.1 Write unit tests in `tests/McpCapabilities.Server.Unit.Tests/CapabilityFlagTests.cs` for: enum has all MCP capability values, `None = 0`, `[Flags]` attribute, `IsSatisfied` method with various inputs
- [x] 2.2 Write unit tests for `FromClientCapabilities` method: Sampling-only client, full client, null client, Tasks with sub-capabilities, Elicitation with Form/Url sub-capabilities
- [x] 2.3 Implement `CapabilityFlag` enum in `src/McpCapabilities.Server/CapabilityFlag.cs`
- [x] 2.4 Implement static `CapabilityFlags` class with `FromClientCapabilities(ClientCapabilities?)` and `IsSatisfied(CapabilityFlag required, CapabilityFlag available)` methods

## 3. RequiredClientCapabilities Attribute (spec: capability-requirements-attribute)

- [x] 3.1 Write unit tests in `RequiredClientCapabilitiesAttributeTests.cs`: attribute targets methods only, has Required and optional Message properties, not inheritable, single application only
- [x] 3.2 Implement `RequiredClientCapabilitiesAttribute` sealed class in `src/McpCapabilities.Server/RequiredClientCapabilitiesAttribute.cs`

## 4. ClientCapabilityRequirements & Meta Storage (spec: meta-storage)

- [x] 4.1 Write unit tests in `ClientCapabilityRequirementsTests.cs`: None singleton, WriteToMeta with flags+message, WriteToMeta with null message, ReadFromMeta populated/null/empty, IsSatisfiedBy with satisfied/unsatisfied/none/null cases
- [x] 4.2 Implement `ClientCapabilityRequirements` readonly record struct with `WriteToMeta`, `ReadFromMeta`, and `IsSatisfiedBy` methods
- [x] 4.3 Write unit tests for `McpServerPrimitiveCapabilityExtensions` (capture/read extension methods for Tool, Prompt, Resource)
- [x] 4.4 Implement extension methods: `CaptureCapabilityRequirements` and `GetCapabilityRequirements` on `McpServerTool`, `McpServerPrompt`, `McpServerResource`

## 5. FluentResults Error Types (spec: fluent-results-errors)

- [x] 5.1 Write unit tests in `CapabilityNotMetErrorTests.cs`: implements `IError`, carries Required/Missing/PrimitiveName properties, has Metadata dictionary, compatible with `Result.Fail<T>()`
- [x] 5.2 Implement `CapabilityNotMetError` class extending `FluentResults.Error` with `WithMetadata` for structured logging

## 6. FilterByClientCapabilities Extension Methods (spec: fluent-results-errors)

- [x] 6.1 Write unit tests in `CapabilityFilteringFluentExtensionsTests.cs`: mixed visible/hidden tools, all hidden returns failure, empty list returns success, hidden tools recorded as reasons, client with full capabilities shows all, client with no capabilities hides all annotated tools
- [x] 6.2 Implement `FilterByClientCapabilities` extension methods for `IList<Tool>`, `IList<Prompt>`, `IList<Resource>` returning `Result<IList<T>>`

## 7. CapabilityFilteringHandlers (spec: capability-filtering-handlers)

- [x] 7.1 Write unit tests in `CapabilityFilteringHandlersTests.cs`: WrapListTools filters unsatisfied tools, preserves satisfied tools, passes through unannotated tools, calls inner handler when provided, returns empty when inner is null, zero-reflection verification
- [x] 7.2 Write unit tests for WrapListPrompts and WrapListResources
- [x] 7.3 Implement `CapabilityFilteringHandlers` static class with `WrapListTools`, `WrapListPrompts`, `WrapListResources` methods

## 8. WithCapabilityAwareTools Registration (spec: capability-aware-tools-registration)

- [x] 8.1 Write unit tests in `CapabilityServerBuilderExtensionsTests.cs`: WithCapabilityAwareTools captures [RequiredClientCapabilities] into _meta, does not modify tools without attribute, configure callback invoked per tool, reflection at registration only
- [x] 8.2 Implement `WithCapabilityAwareTools<T>()` and its overload with configure callback

## 9. AddCapabilityGating Builder Extension (spec: add-capability-gating)

- [x] 9.1 Write unit tests in `AddCapabilityGatingExtensionsTests.cs`: wraps existing handlers, null handlers default to empty, returns IMcpServerBuilder for chaining, all three handlers wrapped, non-list handlers unchanged
- [x] 9.2 Implement `AddCapabilityGating()` extension method in namespace `Microsoft.Extensions.DependencyInjection`

## 10. Integration Tests

- [x] 10.1 Write integration test: real ASP.NET Core host with `AddMcpServer()` + `WithCapabilityAwareTools<AnnotatedTools>()` + `AddCapabilityGating()`, verify `tools/list` response filtered by client capabilities
- [x] 10.2 Write integration test: annotated prompts and resources are filtered correctly
- [x] 10.3 Write integration test: FluentResults `FilterByClientCapabilities` returns correct error metadata for all-hidden scenario

## 11. Quality Gates

- [x] 11.1 Run `dotnet build` — verify zero errors and zero warnings
- [x] 11.2 Run `dotnet test` — verify all unit and integration tests pass
- [x] 11.3 Run code coverage — verify >95% line coverage on `src/McpCapabilities.Server/`
- [x] 11.4 Verify `dotnet pack` produces a valid NuGet package
