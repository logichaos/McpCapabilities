## 1. Project scaffolding

- [ ] 1.1 Create `samples/SampleMcpServer/` directory structure
- [ ] 1.2 Create `samples/SampleMcpServer/SampleMcpServer.csproj` targeting `net10.0` with project reference to `src/McpCapabilities.Server` and package reference to `ModelContextProtocol`
- [ ] 1.3 Add sample project to solution (`McpCapabilities.slnx`) or create a standalone solution

## 2. Tool implementation

- [ ] 2.1 Create `samples/SampleMcpServer/AiTools.cs` with `[McpServerToolType]` attribute
- [ ] 2.2 Add `ai_summarize` tool method annotated with `[McpServerTool]` and `[RequiredClientCapabilities(Required = CapabilityFlag.Sampling, Message = "Requires LLM sampling support")]` — method logs invocation and returns a placeholder string
- [ ] 2.3 Add `echo` tool method annotated with `[McpServerTool]` only (no capability requirement) — method returns the input string verbatim

## 3. Prompt implementation

- [ ] 3.1 Create `samples/SampleMcpServer/HelpfulPrompts.cs` with `[McpServerPromptType]` attribute
- [ ] 3.2 Add `confirm_action` prompt method annotated with `[McpServerPrompt]` and `[RequiredClientCapabilities(Required = CapabilityFlag.Elicitation)]` — method returns a prompt string that guides the LLM to elicit confirmation from the user
- [ ] 3.3 Add `greeting` prompt method annotated with `[McpServerPrompt]` only (no capability requirement) — method returns a simple greeting prompt

## 4. Resource implementation

- [ ] 4.1 Create `samples/SampleMcpServer/WorkspaceResources.cs` with `[McpServerResourceType]` attribute
- [ ] 4.2 Add `workspace_files` resource method annotated with `[McpServerResource]` and `[RequiredClientCapabilities(Required = CapabilityFlag.Roots)]` — method returns a resource URI like `workspace://files` with a description
- [ ] 4.3 Add `app_info` resource method annotated with `[McpServerResource]` only (no capability requirement) — method returns a resource URI like `app://info` with server metadata

## 5. Hosting pipeline

- [ ] 5.1 Create `samples/SampleMcpServer/Program.cs` with `Host.CreateApplicationBuilder` setup
- [ ] 5.2 Register MCP server with `services.AddMcpServer()` and server info (name: "SampleMcpServer", version: "1.0")
- [ ] 5.3 Call `.WithCapabilityAwareTools<AiTools>()` to register tools with capability capture
- [ ] 5.4 Register prompts by adding `McpServerPrompt` instances and manually calling `CaptureCapabilityRequirements()` on each (since no `WithCapabilityAwarePrompts` exists yet) — or use the SDK's `WithPrompts<T>()` and capture via a post-configure option
- [ ] 5.5 Register resources by adding `McpServerResource` instances and manually calling `CaptureCapabilityRequirements()` on each — or use the SDK's `WithResources<T>()` and capture via a post-configure option
- [ ] 5.6 Call `.AddCapabilityGating()` to enable request-time filtering
- [ ] 5.7 Configure stdio transport with `.WithStdioServerTransport()` and run the server

## 6. Documentation

- [ ] 6.1 Create `samples/SampleMcpServer/README.md` explaining what the sample demonstrates, how to build (`dotnet build`), and how to run (`dotnet run`)
- [ ] 6.2 Update `DEVELOPMENT.md` in the repo root to mention the sample project (optional)

## 7. Verification

- [ ] 7.1 Build the sample project — confirm zero warnings and zero errors
- [ ] 7.2 Run `dotnet run` and confirm the server starts and listens on stdio without crashing
- [ ] 7.3 Verify (manually or via integration test) that `tools/list` returns only the gated tools when a client has the required capabilities, and hides them otherwise
