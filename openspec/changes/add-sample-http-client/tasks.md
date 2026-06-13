## 1. Project Scaffolding

- [ ] 1.1 Create `samples/SampleMcpHttpClient/` project directory with `.csproj` targeting `net10.0`, `Microsoft.NET.Sdk`, with `PackageReference` to `ModelContextProtocol`
- [ ] 1.2 Create `tests/SampleMcpHttpClient.Integration.Tests/` project directory with `.csproj` targeting `net10.0`, `Microsoft.NET.Sdk.Web`, `<IsTestProject>true</IsTestProject>`, with `PackageReference` to `ModelContextProtocol`, `TUnit`, and `TUnit.AspNetCore`, and `ProjectReference` to `SampleMcpServer`
- [ ] 1.3 Add both new projects to `McpCapabilities.slnx` solution file
- [ ] 1.4 Verify `Directory.Packages.props` already includes all needed package versions (no new NuGet packages should be needed — `ModelContextProtocol` already provides the HTTP client transport)

## 2. Sample HTTP Client (`samples/SampleMcpHttpClient/`)

- [ ] 2.1 Write `Program.cs`: accept optional `--server-url` argument, create an HTTP-based `McpClient` transport, run full-capability and minimal-capability profiles in sequence, print tool/prompt/resource counts and names to console
- [ ] 2.2 Write `README.md`: document how to start the server (`dotnet run --project samples/SampleMcpServer/`, set `MCP:Transport=http`), then how to run the client, expected output for both profiles

## 3. Integration Tests (`tests/SampleMcpHttpClient.Integration.Tests/`)

- [ ] 3.1 Write a test helper that creates and starts a `WebApplication` for `SampleMcpServer` in HTTP-only mode on a random port, and provides the bound URL
- [ ] 3.2 Write tests for full-capability client over HTTP: connect with Sampling + Roots + Elicitation, assert `ListToolsAsync()` returns `ai_summarize` and `echo`, `ListPromptsAsync()` returns `confirm_action` and `greeting`, `ListResourcesAsync()` returns `workspace_files` and `app_info`
- [ ] 3.3 Write tests for minimal-capability client over HTTP: connect with empty `ClientCapabilities`, assert `ListToolsAsync()` returns only `echo`, `ListPromptsAsync()` returns only `greeting`, `ListResourcesAsync()` returns only `app_info`
- [ ] 3.4 Write a transport-agnostic test that verifies identical primitive visibility between HTTP and stdio for both capability profiles (reuse the existing stdio test pattern for comparison)

## 4. Quality Gates

- [ ] 4.1 Run `dotnet build` on the entire solution — ensure zero errors and zero warnings
- [ ] 4.2 Run `dotnet test` — ensure all new and existing tests pass with no regressions
- [ ] 4.3 Run `dotnet test --coverage` and verify code coverage remains above 95% on `src/` code
