## 1. Project scaffolding

- [ ] 1.1 Create `samples/SampleMcpClient/` directory structure
- [ ] 1.2 Create `samples/SampleMcpClient/SampleMcpClient.csproj` targeting `net10.0` with package reference to `ModelContextProtocol`
- [ ] 1.3 Add client project to solution (`McpCapabilities.slnx`)

## 2. Client implementation

- [ ] 2.1 Create `samples/SampleMcpClient/Program.cs` with `Host.CreateApplicationBuilder` setup
- [ ] 2.2 Implement `RunClientProfile()` helper that takes a `ClientCapabilities` instance, creates an `McpClient` via `StdioClientTransport`, lists tools/prompts/resources, and prints results
- [ ] 2.3 Define and run the "full" client profile with `ClientCapabilities` containing `Sampling`, `Roots`, and `Elicitation`
- [ ] 2.4 Define and run the "minimal" client profile with empty `ClientCapabilities`

## 3. Documentation

- [ ] 3.1 Create `samples/SampleMcpClient/README.md` explaining what the sample demonstrates, how to build, how to run, and expected output for each profile

## 4. Verification

- [ ] 4.1 Build the client project — confirm zero warnings and zero errors
- [ ] 4.2 Build and start the sample server, then run the client — verify the full client sees all 6 primitives and the minimal client sees only 3
