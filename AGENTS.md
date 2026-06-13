# AGENTS.md — Coding Guidelines for AI Agents

## Testing-First Mandate

**Tests must be written before any implementation or production code.** This is non-negotiable.

1. **Write the test first.** Define the expected behavior, inputs, and outputs in a failing test.
2. **Run the test to confirm it fails.** Red phase — the test must fail because the code it tests doesn't exist yet.
3. **Write the minimum implementation to make the test pass.** Green phase — no more, no less.
4. **Refactor.** Clean up both test and production code without changing behavior.

Tests live under `tests/`

### What qualifies as a test?

- **Unit tests** verify a single class or method in isolation. Use mocking/stubbing for dependencies.
- **Integration tests** verify that multiple components work together correctly, including real MCP client/server interactions.
- Every new feature, bug fix, or refactor must be accompanied by at least one new or updated test that exercises the change.

### Test quality expectations

- Tests must be deterministic. No `Thread.Sleep`, no reliance on wall-clock time, no flaky async races.
- Tests must be fast. Unit tests should complete in milliseconds; integration tests in seconds.
- Tests must be independent. No test may depend on the side effects or state left by another test.
- Test names should describe the scenario and expected outcome: `MethodName_Scenario_ExpectedBehavior`.

### Minimal Comments Mandata

- only add comments when they are absolutely necessary
- code should be self-documenting
  - use meaningful names for everything

---

## Quality Gates (Post-Implementation)

After implementing each feature, the following gates **must** pass before the work is considered complete:

1. **Build.** The entire solution must compile with zero errors and zero warnings. Treat warnings as errors is enforced project-wide.
2. **Tests.** All unit and integration tests must pass. No regressions.
3. **Code Coverage.** Code coverage of resource code (i.e., the production code under `src/`, excluding auto-generated code, library code, and test projects) must remain **above 95%**.

### Coverage exclusions

The following are excluded from coverage measurement:
- Auto-generated code (e.g., `*.g.cs`, source generators, designer files).
- Third-party library code and NuGet package references.
- Test projects themselves (`tests/**`).
- Startup/bootstrap boilerplate that cannot be meaningfully tested (e.g., `Program.cs` minimal hosting wire-up), when explicitly marked with `[ExcludeFromCodeCoverage]`.

If coverage drops below 95% due to a new feature, add the missing tests before merging.

---

## Design Principles

### Vertical (Feature) Slicing

Organize code around **features**, not technical layers. A single vertical slice contains everything a feature needs — handler, validation, data access, and tests — colocated rather than spread across arbitrary "layers" folders.

- Prefer: `Features/Auth/LoginHandler.cs`, `Features/Auth/LoginValidator.cs`
- Avoid: `Controllers/`, `Services/`, `Repositories/` dumping grounds that scatter a single feature across the entire codebase.

### Modularity

- Each module/feature should have a single, well-defined responsibility.
- Dependencies between modules must be explicit and minimal. Prefer constructor injection.
- Public APIs of a module should be small and intentional. Keep implementation details internal.
- New capabilities (MCP tools, resources, prompts) should be self-contained modules that can be tested, developed, and reasoned about in isolation.

### Maintainability

- **Readability over cleverness.** Code is read far more often than it is written. Favor clarity.
- **Small, focused classes and methods.** If a method doesn't fit on one screen, split it. If a class has more than one reason to change, split it.
- **Consistent naming.** Follow the existing conventions of the codebase. Don't invent new patterns without strong justification.
- **Delete dead code.** Don't comment it out — remove it. Git history preserves it if needed.
- **XML documentation** on all public APIs. The summary should explain *why*, not *what* (the signature already says what).

### .NET Conventions

- Follow the [.editorconfig](.editorconfig) in the repo root.
- Use file-scoped namespaces.
- Prefer `PrimaryConstructor` parameters over manual constructor boilerplate where it improves clarity.
- Nullable reference types are enabled. Treat warnings as errors.
